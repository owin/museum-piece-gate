using System;
using System.Collections.Generic;
using System.Text;
using Kayak;
using Kayak.Http;

namespace Gate.Kayak
{
    class RequestDelegate : IHttpRequestDelegate
    {
        AppDelegate appDelegate;

        public RequestDelegate(AppDelegate appDelegate)
        {
            this.appDelegate = appDelegate;
        }

        public void OnRequest(HttpRequestHead head, IDataProducer body, IHttpResponseDelegate response)
        {
            var envDict = new Dictionary<string, object>();

            var env = new Environment(envDict);
            env.Headers = head.Headers;
            env.Method = head.Method;
            env.Path = head.Uri;
            env.PathBase = "";
            env.QueryString = ""; // XXX
            env.Scheme = "http"; // XXX
            env.Version = "1.0";
            env.Body = (onData, onError, onEnd) =>
            {
                var d = body.Connect(new DataConsumer(onData, onError, onEnd));
                return () => d.Dispose();
            };

            appDelegate(envDict, (string status, IDictionary<string, string> headers, BodyDelegate bodyDelegate) =>
            {
                response.OnResponse(new HttpResponseHead()
                {
                    Status = status,
                    Headers = headers
                }, new DataProducer(bodyDelegate));
            },
                error =>
                {
                    error.DebugStacktrace();

                    var data = Encoding.ASCII.GetBytes("An error occurred while processing the request.");

                    response.OnResponse(new HttpResponseHead()
                    {
                        Status = "500 Internal Server Error",
                        Headers = new Dictionary<string, string>()
                            {
                                { "Content-Length", data.Length.ToString() },
                                { "Connection", "close" }
                            }
                    },
                    new DataProducer(Delegates.ToDelegate((onData, onError, end) =>
                    {
                        onData(new ArraySegment<byte>(data), null);
                        end();
                        return null;
                    })));
                });
        }
    }
}
