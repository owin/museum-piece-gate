using System;
using System.Collections.Generic;
using System.Text;
using Gate;
using Gate.Owin;

namespace Gate.Middleware
{
    public static partial class BasicAuthExtensions
    {
        public static string GetBasicAuth(this Environment e)
        {
            if (!e.Headers.ContainsKey("authorization"))
                return null;

            string authHeader = e.Headers["authorization"];

            if (!authHeader.StartsWith("Basic "))
                return null;

            string authCred = authHeader.Substring("Basic ".Length);

            return Encoding.ASCII.GetString(Convert.FromBase64String(authCred));
        }

        public static IAppBuilder RequireAuth(this IAppBuilder builder,
            Func<string, bool> authenticates)
        {
            return builder.RequireAuth((e, c) =>
                c(authenticates(e.GetBasicAuth())), "secure");
        }

        public static IAppBuilder RequireAuth(this IAppBuilder builder,
            Action<Environment, Action<bool>> authenticates, string realm)
        {
            return builder.RequireAuth(authenticates,
                b => b.Run(new FourOhOneUnauthorizedResponse(realm).Invoke));
        }

        public static IAppBuilder RequireAuth(this IAppBuilder builder,
            Action<Environment, Action<bool>> authenticates, Action<IAppBuilder> unauthorized)
        {
            return builder.Unless(authenticates, unauthorized);
        }

        class FourOhOneUnauthorizedResponse
        {
            string realm;

            public FourOhOneUnauthorizedResponse(string realm)
            {
                this.realm = realm;
            }

            public void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
            {
                result(
                    "401 Authorization Required",
                    new Dictionary<string, string>()
                    {
                        { "WWW-Authenticate", "Basic Realm=\"" + realm + "\"" }
                    },
                    (Func<ArraySegment<byte>, Action, bool> onData, Action<Exception> onError, Action onComplete) =>
                    {
                        var response = @"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.01 Transitional//EN""
""http://www.w3.org/TR/1999/REC-html401-19991224/loose.dtd"">
<HTML>
  <HEAD>
    <TITLE>Error</TITLE>
  </HEAD>
  <BODY><H1>401 Unauthorized.</H1></BODY>
</HTML>";
                        onData(new ArraySegment<byte>(Encoding.ASCII.GetBytes(response)), null);
                        onComplete();
                        return null;
                    });
            }
        }
    }
}
