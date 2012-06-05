using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Owin;
using Kayak;
using Kayak.Http;

namespace Gate.Hosts.Kayak
{
    class GateRequestDelegate : IHttpRequestDelegate
    {
        AppDelegate appDelegate;
        IDictionary<string, object> context;

        public GateRequestDelegate(AppDelegate appDelegate, IDictionary<string, object> context)
        {
            this.appDelegate = appDelegate;
            this.context = context;
        }

        public void OnRequest(HttpRequestHead head, IDataProducer body, IHttpResponseDelegate response)
        {
            var env = new Dictionary<string, object>();
            var request = new RequestEnvironment(env);

            if (context != null)
                foreach (var kv in context)
                    env[kv.Key] = kv.Value;

            if (head.Headers == null)
                request.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            else
                request.Headers = head.Headers.ToDictionary(kv => kv.Key, kv => new[] { kv.Value }, StringComparer.OrdinalIgnoreCase);

            request.Method = head.Method ?? "";
            request.Path = head.Path ?? "";
            request.PathBase = "";
            request.QueryString = head.QueryString ?? "";
            request.Scheme = "http"; // XXX
            request.Version = "1.0";

            if (body == null)
                request.BodyDelegate = null;
            else
                request.BodyDelegate = (write, end, cancellationToken) =>
                {
                    var d = body.Connect(new DataConsumer(
                        write,
                        end,
                        () => end(null)));
                    cancellationToken.Register(d.Dispose);
                };

            appDelegate(env, HandleResponse(response), HandleError(response));
        }

        ResultDelegate HandleResponse(IHttpResponseDelegate response)
        {
            return (status, headers, body) =>
            {
                if (headers == null)
                    headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

                if (body != null &&
                    !headers.ContainsKey("Content-Length") &&
                    !headers.ContainsKey("Transfer-Encoding"))
                {
                    // disable keep-alive in this case
                    headers["Connection"] = new[] {"close"};
                }

                response.OnResponse(new HttpResponseHead()
                    {
                        Status = status,
                        Headers = headers.ToDictionary(kv => kv.Key, kv => string.Join("\r\n", kv.Value.ToArray()), StringComparer.OrdinalIgnoreCase),
                    }, body == null ? null : new DataProducer(body));                
            };
        }

        Action<Exception> HandleError(IHttpResponseDelegate response)
        {
            return error =>
            {
                Console.Error.WriteLine("Error from Gate application.");
                Console.Error.WriteStackTrace(error);

                response.OnResponse(new HttpResponseHead()
                {
                    Status = "503 Internal Server Error",
                    Headers = new Dictionary<string, string>()
                    {
                        { "Connection", "close" }
                    }
                }, null);
            };
        }
    }
}
