using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Gate.Hosts
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    static class ErrorPage
    {
        static readonly ArraySegment<byte> Body = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
@"
<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML 2.0//EN"">
<html><head>
<title>500 Internal Server Error</title>
</head><body>
<h1>Internal Server Error</h1>
<p>The requested URL was not found on this server.</p>
</body></html>
"));

        public static AppFunc Middleware(AppFunc app)
        {
            return Middleware(app, ex => { });
        }

        public static AppFunc Middleware(AppFunc app, Action<Exception> logError)
        {
            return env =>
            {
                try
                {
                    return app(env)
                        .Catch(errorInfo =>
                        {
                            SetErrorResponse(env);
                            return errorInfo.Handled();
                        });
                }
                catch (Exception ex)
                {
                    logError(ex);
                    SetErrorResponse(env);
                    return TaskHelpers.Completed();
                }
            };
        }

        private static void SetErrorResponse(IDictionary<string, object> env)
        {
            Response response = new Response(env);
            response.StatusCode = 500;
            response.ReasonPhrase = "Internal Server Error";
            response.ContentType = "text/html";
            response.Write(Body);
        }
    }
}
