using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Owin;
using System.Threading.Tasks;

namespace Gate.Hosts
{
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

        public static AppDelegate Middleware(AppDelegate app)
        {
            return Middleware(app, ex => { });
        }

        public static AppDelegate Middleware(AppDelegate app, Action<Exception> logError)
        {
            return call =>
            {
                try
                {
                    return app(call)
                        .Then(result =>
                        {
                            if (result.Body != null)
                            {
                                var nestedBody = result.Body;
                                result.Body = stream =>
                                {
                                    try
                                    {
                                        return nestedBody(stream)
                                            .Catch(errorInfo =>
                                            {
                                                logError(errorInfo.Exception);
                                                return errorInfo.Handled();
                                            });
                                    }
                                    catch (Exception ex)
                                    {
                                        logError(ex);
                                        return TaskHelpers.Completed();
                                    }
                                };
                            }

                            return result;
                        })
                        .Catch(errorInfo =>
                        {
                            return errorInfo.Handled(CreateErrorResponse());
                        });
                }
                catch (Exception ex)
                {
                    logError(ex);
                    return TaskHelpers.FromResult(CreateErrorResponse());
                }
            };
        }

        private static ResultParameters CreateErrorResponse()
        {
            return new ResultParameters()
            {
                Status = 500,
                Properties = new Dictionary<string, object>()
                {
                    { "owin.ReasonPhrase", "Internal Server Error" }
                },
                Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                     { "Content-Type", new[] { "text/html" } }
                },
                Body = stream =>
                {
                    stream.Write(Body.Array, Body.Offset, Body.Count);
                    return TaskHelpers.Completed();
                },
            };
        }
    }
}
