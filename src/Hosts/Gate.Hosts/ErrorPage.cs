using System;
using System.Collections.Generic;
using System.Text;
using Owin;

namespace Gate.Hosts
{
    static class ErrorPage
    {
        static readonly ArraySegment<byte> Body = new ArraySegment<byte>(Encoding.UTF8.GetBytes(@"
<!DOCTYPE HTML PUBLIC ""-//IETF//DTD HTML 2.0//EN"">
<html><head>
<title>500 Internal Server Error</title>
</head><body>
<h1>Internal Server Error</h1>
<p>The requested URL was not found on this server.</p>
</body></html>
"));
        static readonly Dictionary<string, string[]> ResponseHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { { "Content-Type", new[] { "text/html" } } };

        public static AppDelegate Middleware(AppDelegate app)
        {
            return Middleware(app, ex => { });
        }

        public static AppDelegate Middleware(AppDelegate app, Action<Exception> logError)
        {
            return (env, result, fault) =>
            {
                Action<Exception> onError = ex =>
                {
                    logError(ex);
                    result(
                        "500 Internal Server Error",
                        ResponseHeaders,
                        (write, end, cancel) =>
                        {
                            try
                            {
                                write(Body, null);
                                end(null);
                            }
                            catch (Exception error)
                            {
                                end(error);
                            }
                        });
                };

                try
                {
                    app(
                        env,
                        (status, headers, body) =>
                        {
                            // errors send from inside the body are
                            // logged, but not passed to the host. it's too
                            // late to change the status or send an error page.
                            onError = logError;
                            result(
                                status,
                                headers,
                                (write, end, cancel) =>
                                    body(
                                        write,
                                        ex =>
                                        {
                                            if (ex != null)
                                            {
                                                logError(ex);
                                            }
                                            end(ex);
                                        },
                                        cancel));
                        },
                        ex => onError(ex));
                }
                catch (Exception ex)
                {
                    onError(ex);
                }
            };
        }
    }
}
