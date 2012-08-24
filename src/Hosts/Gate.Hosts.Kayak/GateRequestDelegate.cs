using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Kayak;
using Kayak.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace Gate.Hosts.Kayak
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using System.IO;

    class GateRequestDelegate : IHttpRequestDelegate
    {
        AppFunc appFunc;
        IDictionary<string, object> context;

        public GateRequestDelegate(AppFunc appFunc, IDictionary<string, object> context)
        {
            this.appFunc = appFunc;
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
                env[OwinConstants.RequestHeaders] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            else
                env[OwinConstants.RequestHeaders] = head.Headers.ToDictionary(kv => kv.Key, kv => new[] { kv.Value }, StringComparer.OrdinalIgnoreCase);

            env[OwinConstants.ResponseHeaders] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            request.Method = head.Method ?? "";
            request.Path = head.Path ?? "";
            request.PathBase = "";
            request.QueryString = head.QueryString ?? "";
            request.Scheme = "http"; // XXX
            request.Version = "1.0";

            if (body == null)
                env[OwinConstants.RequestBody] = Stream.Null;
                /*
            else
                request.Body = (write, end, cancellationToken) =>
                {
                    var d = body.Connect(new DataConsumer(
                        write,
                        end,
                        () => end(null)));
                    cancellationToken.Register(d.Dispose);
                };
                 */ // TODO: Request body stream

            appFunc(env)
                .Then(() => HandleResponse(response, env))
                .Catch(errorInfo => HandleError(response, errorInfo));
        }

        private void HandleResponse(IHttpResponseDelegate response, IDictionary<string, object> env)
        {
            var headers = (IDictionary<string, string[]>)env[OwinConstants.ResponseHeaders];
            if (!headers.ContainsKey("Content-Length") &&
                !headers.ContainsKey("Transfer-Encoding"))
            {
                // disable keep-alive in this case
                headers["Connection"] = new[] { "close" };
            }

            response.OnResponse(new HttpResponseHead()
                {
                    Status = GetStatus(env),
                    Headers = headers.ToDictionary(kv => kv.Key, kv => string.Join("\r\n", kv.Value.ToArray()), StringComparer.OrdinalIgnoreCase),
                }, null /* result.Body == null ? null : new DataProducer(result.Body) */); // TODO: How do we expose DataProducer as a Stream?
        }

        private string GetStatus(IDictionary<string, object> env)
        {
            string status = "200";
            object obj = null;
            if (env.TryGetValue(OwinConstants.ResponseStatusCode, out obj))
            {
                status = ((int)obj).ToString(CultureInfo.InvariantCulture);
            }

            obj = null;
            if (env.TryGetValue(OwinConstants.ResponseReasonPhrase, out obj))
            {
                string reason = (string)obj;
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    status += " " + reason;
                }
            }
            return status;
        }

        private CatchInfoBase<Task>.CatchResult HandleError(IHttpResponseDelegate response, CatchInfo errorInfo)
        {
            Console.Error.WriteLine("Error from Gate application.");
            Console.Error.WriteStackTrace(errorInfo.Exception);

            response.OnResponse(new HttpResponseHead()
            {
                Status = "503 Internal Server Error",
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Connection", "close" }
                }
            }, null);
            return errorInfo.Handled();
        }
    }
}
