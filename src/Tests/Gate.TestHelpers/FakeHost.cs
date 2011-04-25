using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Gate.Startup;
using Nancy.Hosting.Owin.Tests.Fakes;

namespace Gate.TestHelpers
{
    public class FakeHost
    {
        readonly AppDelegate _app;

        public FakeHost(string configurationString)
        {
            _app = new AppBuilder().Configure(configurationString).Build();
        }

        public FakeHost(AppDelegate app)
        {
            _app = app;
        }

        public FakeHostResponse GET(string path)
        {
            return GET(path, request => { });
        }

        public FakeHostResponse GET(string path, Action<FakeHostRequest> requestSetup)
        {
            var pathParts = path.Split("?".ToArray(), 2);
            return Invoke(request =>
            {
                request.Method = "GET";
                request.PathBase = "";
                request.Path = pathParts[0];
                request.QueryString = pathParts.Length == 2 ? pathParts[1] : null;
                requestSetup(request);
            });
        }

        FakeHostResponse Invoke(Action<FakeHostRequest> requestSetup)
        {
            var env = new Dictionary<string, object>();

            var request = new FakeHostRequest(env)
            {
                Version = "1.0",
                Scheme = "http",
                Headers = new Dictionary<string, string>(),
            };
            requestSetup(request);

            var wait = new ManualResetEvent(false);
            var response = new FakeHostResponse();
            _app(
                env,
                (status, headers, body) =>
                {
                    response.Status = status;
                    response.Headers = headers;
                    response.Body = body;
                    response.Consumer = new FakeConsumer(true);
                    response.Consumer.InvokeBodyDelegate(body, true);

                    string contentType;
                    if (!headers.TryGetValue("Content-Type", out contentType))
                        contentType = "";

                    if (contentType.StartsWith("text/"))
                    {
                        response.BodyText = Encoding.UTF8.GetString(response.Consumer.ConsumedData);
                        if (contentType.StartsWith("text/xml"))
                        {
                            response.BodyXml = XElement.Parse(response.BodyText);
                        }
                    }
                    wait.Set();
                },
                ex =>
                {
                    response.Exception = ex;
                    wait.Set();
                });
            wait.WaitOne();
            return response;
        }
    }
}