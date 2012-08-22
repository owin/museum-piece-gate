using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Middleware;
using Owin;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Owin
{
    public static class TracerExtensions
    {
        public static IAppBuilder UseRequestTracer(this IAppBuilder builder)
        {
            TraceSource traceSource = builder.Properties.Get<TraceSource>("host.TraceSource");
            return builder.UseType<RequestTracer>(traceSource);
        }
    }
}

namespace Gate.Middleware
{
    // This middleware traces incoming and outgoing environment and properties variables, headers, etc.
    public class RequestTracer
    {
        private readonly AppDelegate nextApp;
        private TraceSource traceSource;

        public RequestTracer(AppDelegate next)
        {
            nextApp = next;
        }

        public RequestTracer(AppDelegate next, TraceSource source)
        {
            nextApp = next;
            traceSource = source;
        }

        public Task<ResultParameters> Invoke(CallParameters call)
        {
            // The TraceSource is assumed to be the same across all requests.
            traceSource = traceSource ?? call.Environment.Get<TraceSource>("host.TraceSource");

            if (traceSource == null)
            {
                return nextApp(call);
            }

            try
            {
                TraceCall(call);
                return nextApp(call).Then(result =>
                {
                    TraceResult(result);
                    return result;
                })
                .Catch(errorInfo =>
                {
                    TraceException(errorInfo.Exception, "asynchronously");
                    return errorInfo.Throw();
                });
            }
            catch (Exception ex)
            {
                TraceException(ex, "synchronously");
                throw;
            }
        }

        private void TraceCall(CallParameters call)
        {
            traceSource.TraceEvent(TraceEventType.Start, 0, "Request: Environment#{0}, Headers#{1}, Body={2};", 
                call.Environment.Count, call.Headers.Count, 
                (call.Body == null ? "(null)" : call.Body.GetType().FullName));

            if (call.Environment.Count > 0)
            {
                traceSource.TraceInformation("Environment: ");
                TraceDictionary(call.Environment);
            }

            if (call.Headers.Count > 0)
            {
                traceSource.TraceInformation("Headers: ");
                TraceHeaders(call.Headers);
            }
        }

        private void TraceResult(ResultParameters result)
        {
            traceSource.TraceEvent(TraceEventType.Stop, 0, "Response: Status#{0}, Properties#{1}, Headers#{2}, Body={3};",
                result.Status, result.Properties.Count, result.Headers.Count,
                (result.Body == null ? "(null)" : result.Body.GetType().FullName));

            if (result.Properties.Count > 0)
            {
                traceSource.TraceInformation("Properties: ");
                TraceDictionary(result.Properties);
            }

            if (result.Headers.Count > 0)
            {
                traceSource.TraceInformation("Headers: ");
                TraceHeaders(result.Headers);
            }
        }

        private void TraceDictionary(IDictionary<string, object> dictionary)
        {
            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                traceSource.TraceData(TraceEventType.Verbose, 0, 
                    string.Format("{0} - T:{1}, V:{2}", pair.Key,
                    (pair.Value == null ? "(null)" : pair.Value.GetType().FullName),
                    (pair.Value == null ? "(null)" : pair.Value.ToString())));
            }
        }

        private void TraceHeaders(IDictionary<string, string[]> headers)
        {
            foreach (KeyValuePair<string, string[]> header in headers)
            {
                foreach (string value in header.Value)
                {
                    traceSource.TraceData(TraceEventType.Verbose, 0,
                        string.Format("{0}: {1}", header.Key,
                        (value == null ? "(null)" : value)));
                }
            }
        }

        private void TraceException(Exception exception, string place)
        {
            traceSource.TraceEvent(TraceEventType.Error, 0, "An exception was thrown {0}: {1}", place, exception);
        }
    }
}
