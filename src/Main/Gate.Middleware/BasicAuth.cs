using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owin;

namespace Gate.Middleware
{
    public static class BasicAuth
    {
        public static IAppBuilder UseBasicAuth(this IAppBuilder builder, string realm, Func<string, string, bool> authenticator)
        {
            return builder.Use(Middleware, realm, authenticator);
        }

        public static AppDelegate Middleware(AppDelegate app, string realm, Func<string, string, bool> authenticator)
        {
            return (env, result, fault) =>
            {
                if ((!env.ContainsKey("gate.RemoteUser") || env["gate.RemoteUser"] == null))
                {
                    Authenticate(app, env, result, fault, realm, authenticator);
                }
                else
                {
                    // Should be authenticated.  Move along.
                    if (app != null)
                    {
                        app(env, result, fault);
                    }
                }
            };
        }

        private static void Authenticate(AppDelegate app, 
            IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault,
            string realm, Func<string, string, bool> authenticator)
        {
            const string authorizationKey = "Authorization";
            var environment = new Environment(env);

            if (!environment.Headers.ContainsKey(authorizationKey))
            {
                Unauthorized(result, realm);
                return;
            }

            var authorizationHeader = environment.Headers[authorizationKey].ToArray().First();
            var headerValues = authorizationHeader.Split(new[] {' '}, 2);
            var scheme = headerValues[0];
            var param = headerValues[1];

            if (!scheme.Equals("Basic", StringComparison.InvariantCultureIgnoreCase))
            {
                var response = new Response(result, "400 Bad Request");
                response.End();
                return;
            }

            var credentials = Encoding.ASCII.GetString(Convert.FromBase64String(param)).Split(new[] {':'}, 2);
            var username = credentials[0];
            var password = credentials[1];

            if (authenticator(username, password))
            {
                env["gate.RemoteUser"] = username;
                app(env, result, fault);
                return;
            }

            Unauthorized(result, realm);
        }

        private static void Unauthorized(ResultDelegate result, string realm)
        {
            const string status = "401 Unauthorized";
            var headers = Headers.New();
            var challenge = string.Format("Basic realm=\"{0}\"", realm);
            headers.Add("WWW-Authenticate", new[] { challenge });

            var response = new Response(result) {Status = status, Headers = headers};
            response.End();
        }
    }
}