using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Gate.Helpers;
using Nancy.Hosting.Owin.Tests.Fakes;

namespace Gate.TestHelpers
{
    public class AppUtils
    {
        public static CallResult Call(AppDelegate app)
        {
            return Call(app, "");
        }

        public static CallResult Call(AppDelegate app, string path)
        {
            var env = new Dictionary<string, object>();
            var pathParts = path.Split("?".ToArray(), 2);
            new Environment(env)
            {
                Version = "1.0",
                Path = pathParts[0],
                QueryString = pathParts.Length == 2 ? pathParts[1] : null,
            };
            var wait = new ManualResetEvent(false);
            var callResult = new CallResult();
            app(
                env,
                (string status, IDictionary<string, string> headers, BodyDelegate body) =>
                {
                    callResult.Status = status;
                    callResult.Headers = headers;
                    callResult.Body = body;

                    callResult.Consumer = new FakeConsumer(true);
                    callResult.Consumer.InvokeBodyDelegate(callResult.Body, true);

                    string contentType;
                    if (!headers.TryGetValue("Content-Type", out contentType))
                        contentType = "";

                    if (contentType.StartsWith("text/"))
                    {
                        callResult.BodyText = Encoding.UTF8.GetString(callResult.Consumer.ConsumedData);
                        if (contentType.StartsWith("text/xml"))
                        {
                            callResult.BodyXml = XElement.Parse(callResult.BodyText);
                        }
                    }

                    wait.Set();
                },
                exception =>
                {
                    callResult.Exception = exception;
                    wait.Set();
                });
            wait.WaitOne();
            return callResult;
        }

        public static AppDelegate ShowEnvironment()
        {
            return (env, result, fault) =>
            {
                var response = new Response(result) {Status = "200 OK", ContentType = "text/xml"};
                response.Finish((error, complete) =>
                {
                    var detail = env.Select(kv => new XElement(kv.Key, kv.Value));
                    var xml = new XElement("xml", detail.OfType<object>().ToArray());
                    response.Write(xml.ToString());
                    complete();
                });
            };
        }

        public static AppDelegate Simple(string status, IDictionary<string, string> headers, string body)
        {
            return new FakeApp(status, body) {Headers = headers}.AppDelegate;
        }
    }

    public class CallResult
    {
        public string Status { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public BodyDelegate Body { get; set; }
        public string BodyText { get; set; }
        public XElement BodyXml { get; set; }

        public FakeConsumer Consumer { get; set; }
        public Exception Exception { get; set; }
    }
}