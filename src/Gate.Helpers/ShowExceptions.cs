using System;
using System.Collections.Generic;

namespace Gate.Helpers
{
    using AppDelegate = Action< // app
        IDictionary<string, object>, // env
        Action< // result
            string, // status
            IDictionary<string, string>, // headers
            Func< // body
                Func< // next
                    ArraySegment<byte>, // data
                    Action, // continuation
                    bool>, // async                    
                Action<Exception>, // error
                Action, // complete
                Action>>, // cancel
        Action<Exception>>; // error

    public class ShowExceptions
    {
        public static AppDelegate Create(AppDelegate app)
        {
            return (env, result, fault) =>
            {
                Action<Exception> show = ex =>
                {
                    var response = new Response(result)
                    {
                        Status = "500 ERROR",
                        ContentType = "text/html",
                    };
                    response.Finish((error, complete) =>
                    {
                        response.Write("<h1>Server Error</h1>");
                        response.Write(ex.Message);
                        complete();
                    });
                };

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
    }
}