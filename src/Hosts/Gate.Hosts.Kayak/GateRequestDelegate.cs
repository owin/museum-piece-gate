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

        Action<ResultParameters, Exception> HandleResponse(IHttpResponseDelegate response)
        {
            return (result, error) =>
            {
                if (error != null)
                {
                    HandleError(response).Invoke(error);
                    return;
                }

                if (result.Headers == null)
                {
                    result.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                }

                if (result.Body != null &&
                    !result.Headers.ContainsKey("Content-Length") &&
                    !result.Headers.ContainsKey("Transfer-Encoding"))
                {
                    // disable keep-alive in this case
                    result.Headers["Connection"] = new[] { "close" };
                }

                response.OnResponse(new HttpResponseHead()
                    {
                        Status = GetStatus(result),
                        Headers = result.Headers.ToDictionary(kv => kv.Key, kv => string.Join("\r\n", kv.Value.ToArray()), StringComparer.OrdinalIgnoreCase),
                    }, result.Body == null ? null : new DataProducer(result.Body));
            };
        }

        private string GetStatus(ResultParameters result)
        {
            throw new NotImplementedException();
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
