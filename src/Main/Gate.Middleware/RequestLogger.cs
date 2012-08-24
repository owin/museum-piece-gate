using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owin;
using System.Threading.Tasks;
using Gate.Middleware;
using System.IO;
using Gate;

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
    using AppFunc = Func<IDictionary<string, object>, Task>;

    // This middleware logs incoming and outgoing environment and properties variables, headers, etc.
    public class RequestLogger
    {
        private readonly AppFunc nextApp;
        private TextWriter logger;

        public RequestLogger(AppFunc next)
        {
            nextApp = next;
        }

        public RequestLogger(AppFunc next, TextWriter logger)
        {
            nextApp = next;
            this.logger = logger;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            // The TextWriter is assumed to be the same across all requests.
            logger = logger ?? env.Get<TextWriter>(OwinConstants.TraceOutput);

            if (logger == null)
            {
                return nextApp(env);
            }

            LogCall(env);
            return nextApp(env).Then(() =>
            {
                LogResult(env);
            });
        }

        private void LogCall(IDictionary<string, object> env)
        {
            logger.WriteLine("{0} - Request: Environment#{1}", DateTime.Now, env.Count);

            logger.WriteLine("Environment: ");
            LogDictionary(env);

            logger.WriteLine("Headers: ");
            LogHeaders(env.Get<IDictionary<string, string[]>>(OwinConstants.RequestHeaders));
        }

        private void LogResult(IDictionary<string, object> env)
        {
            logger.WriteLine("{0} - Response: Environment#{1}", DateTime.Now, env.Count);

            logger.WriteLine("Environment: ");
            LogDictionary(env);

            logger.WriteLine("Headers: ");
            LogHeaders(env.Get<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders));
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
                foreach (string value in header.Value)
                {
                    logger.WriteLine("{0}: {1}", header.Key, value ?? "(null)");
                }
            }
        }
    }
}
