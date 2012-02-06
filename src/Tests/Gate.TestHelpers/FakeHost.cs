using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Gate.Builder;
using Owin;

namespace Gate.TestHelpers
{
    public class FakeHost
    {
        readonly AppDelegate _app;

        public FakeHost(string configurationString)
        {
            _app = AppBuilder.BuildConfiguration(configurationString);
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
            return Invoke(FakeHostRequest.GetRequest(path, requestSetup));
        }

        public FakeHostResponse Invoke(Action<FakeHostRequest> requestSetup)
        {
            var request = new FakeHostRequest()
            {
                Version = "1.0",
                Scheme = "http",
                Headers = Headers.New(),
            };
            requestSetup(request);

            var wait = new ManualResetEvent(false);
            var response = new FakeHostResponse();
            _app(
                request,
                (status, headers, body) =>
                {
                    response.Status = status;
                    response.Headers = headers;
                    if (body != null)
                    {
                        response.Body = body;
                        response.Consumer = new FakeConsumer(true);
                        response.Consumer.InvokeBodyDelegate(body, true);

                        var contentType = headers.GetHeader("Content-Type");
    
                        if (contentType != null && contentType.StartsWith("text/"))
                        {
                            response.BodyText = Encoding.UTF8.GetString(response.Consumer.ConsumedData);
                            if (contentType.StartsWith("text/xml"))
                            {
                                response.BodyXml = XElement.Parse(response.BodyText);
                            }
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