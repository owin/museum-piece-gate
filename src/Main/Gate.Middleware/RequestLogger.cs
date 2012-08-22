using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owin;
using System.Threading.Tasks;
using Gate.Middleware;
using System.IO;

namespace Owin
{
    public static class LoggerExtensions
    {
        public static IAppBuilder UseRequestLogger(this IAppBuilder builder)
        {
            TextWriter logger = builder.Properties.Get<TextWriter>(OwinConstants.TraceOutput);
            return builder.UseType<RequestLogger>(logger);
        }
    }
}

namespace Gate.Middleware
{
    // This middleware logs incoming and outgoing environment and properties variables, headers, etc.
    public class RequestLogger
    {
        private readonly AppDelegate nextApp;
        private TextWriter logger;

        public RequestLogger(AppDelegate next)
        {
            nextApp = next;
        }

        public RequestLogger(AppDelegate next, TextWriter logger)
        {
            nextApp = next;
            this.logger = logger;
        }

        public Task<ResultParameters> Invoke(CallParameters call)
        {
            // The TextWriter is assumed to be the same across all requests.
            logger = logger ?? call.Environment.Get<TextWriter>(OwinConstants.TraceOutput);

            if (logger == null)
            {
                return nextApp(call);
            }

            LogCall(call);
            return nextApp(call).Then(result =>
            {
                LogResult(result);
                return result;
            });
        }

        private void LogCall(CallParameters call)
        {
            logger.WriteLine("{0} - Request: Environment#{1}, Headers#{2}, Body={3};", 
                DateTime.Now, call.Environment.Count, call.Headers.Count, 
                (call.Body == null ? "(null)" : call.Body.GetType().FullName));

            if (call.Environment.Count > 0)
            {
                logger.WriteLine("Environment: ");
                LogDictionary(call.Environment);
            }

            if (call.Headers.Count > 0)
            {
                logger.WriteLine("Headers: ");
                LogHeaders(call.Headers);
            }
        }

        private void LogResult(ResultParameters result)
        {
            logger.WriteLine("{0} - Response: Status#{1}, Properties#{2}, Headers#{3}, Body={4};",
                DateTime.Now, result.Status, result.Properties.Count, result.Headers.Count,
                (result.Body == null ? "(null)" : result.Body.GetType().FullName));

            if (result.Properties.Count > 0)
            {
                logger.WriteLine("Properties: ");
                LogDictionary(result.Properties);
            }

            if (result.Headers.Count > 0)
            {
                logger.WriteLine("Headers: ");
                LogHeaders(result.Headers);
            }
        }

        private void LogDictionary(IDictionary<string, object> dictionary)
        {
            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                logger.WriteLine("{0} - T:{1}, V:{2}", pair.Key,
                    (pair.Value == null ? "(null)" : pair.Value.GetType().FullName),
                    (pair.Value == null ? "(null)" : pair.Value.ToString()));
            }
        }

        private void LogHeaders(IDictionary<string, string[]> headers)
        {
            foreach (KeyValuePair<string, string[]> header in headers)
            {
                logger.WriteLine("{0}: {1}", header.Key,
                    (header.Value == null ? "(null)" : string.Join(", ", header.Value)));
            }
        }
    }
}
