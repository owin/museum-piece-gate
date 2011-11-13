using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Owin;

namespace Gate.Middleware
{
    public static class ForwardedFor
    {
        public static IAppBuilder UseForwardedFor(this IAppBuilder builder, params string[] knownHttpProxies)
        {
            return builder.Use(Middleware, knownHttpProxies);
        }

        public static AppDelegate Middleware(AppDelegate app, IEnumerable<string> knownHttpProxies)
        {
            return (env, result, fault) =>
            {
                var req = new Request(env);
                var clientIp = GetClientIp(req);
                var forwardedFor = GetForwardedFor(req);

                for (; ; )
                {
                    if (clientIp != null && knownHttpProxies.Contains(clientIp))
                        clientIp = null;

                    if (clientIp != null)
                        break;

                    if (forwardedFor == null)
                        break;

                    var finalDelimiter = forwardedFor.LastIndexOfAny(new[] { '\r', '\n', ',' });
                    clientIp = forwardedFor.Substring(finalDelimiter + 1).Trim();
                    forwardedFor = finalDelimiter < 1 ? null : forwardedFor.Substring(0, finalDelimiter).Trim();
                }

                if (clientIp != null && forwardedFor != null)
                {
                    forwardedFor = forwardedFor + "," + clientIp;
                }
                else 
                {
                    forwardedFor = clientIp;
                }

                if (req.ContainsKey("server.CLIENT_IP"))
                {
                    req.Remove("server.CLIENT_IP");
                }
                if (string.IsNullOrWhiteSpace(forwardedFor))
                {
                    if (req.Headers.ContainsKey("X-Forwarded-For"))
                        req.Headers.Remove("X-Forwarded-For");
                }
                else
                {
                    req.Headers["X-Forwarded-For"] = forwardedFor;
                }
                app(env, result, fault);
            };
        }

        static string GetClientIp(IDictionary<string, object> req)
        {
            object value;
            if (req.TryGetValue("server.CLIENT_IP", out value))
            {
                var clientIp = Convert.ToString(value);
                if (!string.IsNullOrWhiteSpace(clientIp))
                    return clientIp;
            }
            return null;
        }

        static string GetForwardedFor(Request req)
        {
            var headers = req.Headers;
            if (headers == null)
                return null;

            string forwardedFor;
            if (headers.TryGetValue("X-Forwarded-For", out forwardedFor))
            {
                if (!string.IsNullOrWhiteSpace(forwardedFor))
                    return forwardedFor;
            }
            return null;
        }
    }
}
