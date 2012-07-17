using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Owin;
using Kayak;
using Kayak.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

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
            var request = new CallParameters();
            request.Completed = CancellationToken.None; // TODO:
            request.Environment = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var requestWrapper = new RequestEnvironment(request.Environment);

            if (context != null)
                foreach (var kv in context)
                    request.Environment[kv.Key] = kv.Value;

            if (head.Headers == null)
                request.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            else
                request.Headers = head.Headers.ToDictionary(kv => kv.Key, kv => new[] { kv.Value }, StringComparer.OrdinalIgnoreCase);

            requestWrapper.Method = head.Method ?? "";
            requestWrapper.Path = head.Path ?? "";
            requestWrapper.PathBase = "";
            requestWrapper.QueryString = head.QueryString ?? "";
            requestWrapper.Scheme = "http"; // XXX
            requestWrapper.Version = "1.0";

            if (body == null)
                request.Body = null;
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

            appDelegate(request)
                .Then(result => HandleResponse(response, result))
                .Catch(errorInfo => HandleError(response, errorInfo));
        }

        private void HandleResponse(IHttpResponseDelegate response, ResultParameters result)
        {
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
                }, null /* result.Body == null ? null : new DataProducer(result.Body) */); // TODO: How do we expose DataProducer as a Stream?
        }

        private string GetStatus(ResultParameters result)
        {
            string status = result.Status.ToString(CultureInfo.InvariantCulture);

            object obj;
            if (result.Properties != null && result.Properties.TryGetValue(OwinConstants.ReasonPhrase, out obj))
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
                Headers = new Dictionary<string, string>()
                {
                    { "Connection", "close" }
                }
            }, null);
            return errorInfo.Handled();
        }
    }
}
