using System;
using System.Collections.Generic;
using System.Text;

namespace Gate.Helpers
{
    public partial class ShowExceptions : IMiddleware
    {
        AppDelegate IMiddleware.Create(AppDelegate app)
        {
            return Create(app);
        }

        public static AppDelegate Create(AppDelegate app)
        {
            return (env, result, fault) =>
            {
                Action<Exception, Func<ArraySegment<byte>, Action, bool>, Action> showErrorMessage = (ex, next, complete) =>
                {
                    ErrorPage(env, ex, text => next(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), null));
                    complete();
                };

                Action<Exception> showErrorPage = ex =>
                    new Response(result) {Status = "500 Internal Server Error", ContentType = "text/html"}
                        .Finish((response, error, complete) =>
                            showErrorMessage(ex, response.WriteAsync, complete));

                try
                {
                    app(
                        env,
                        (status, headers, body) =>
                            result(
                                status,
                                headers,
                                (next, error, complete) =>
                                    body(
                                        next,
                                        ex => showErrorMessage(ex, next, complete),
                                        complete)),
                        showErrorPage);
                }
                catch (Exception exception)
                {
                    showErrorPage(exception);
                }
            };
        }

        static string h(object text)
        {
            return Convert.ToString(text);
        }
    }
}