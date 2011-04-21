using System;
using System.Collections.Generic;

namespace Gate.Helpers
{
    public partial class ShowExceptions
    {
        public static AppDelegate Create(AppDelegate app)
        {
            return (env, result, fault) =>
            {
                Action<Exception> show = ex =>
                    new Response(result) {Status = "500 Internal Server Error", ContentType = "text/html"}
                        .Finish((response, error, complete) =>
                        {
                            ErrorPage(env, response, ex);
                            complete();
                        });

                try
                {
                    app(env, result, show);
                }
                catch (Exception exception)
                {
                    show(exception);
                }
            };
        }

        static string h(object text)
        {
            return Convert.ToString(text);
        }
    }
}