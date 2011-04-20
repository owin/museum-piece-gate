using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    /// <summary>
    /// Sets content type in response if none present
    /// </summary>
    public class ContentType : IMiddleware, IMiddleware<string>
    {        
        const string DefaultContentType = "text/html";

        AppDelegate IMiddleware.Create(AppDelegate app)
        {
            return Create(app, DefaultContentType);
        }

        AppDelegate IMiddleware<string>.Create(AppDelegate app, string contentType)
        {
            return Create(app, contentType);
        }

        public static AppDelegate Create(AppDelegate app)
        {
            return Create(app, DefaultContentType);
        }

        public static AppDelegate Create(AppDelegate app, string contentType)
        {
            return (env, result, fault) => app(
                env,
                (status, headers, body) =>
                {
                    if (!headers.Any(kv => string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)))
                        headers.Add("Content-Type", contentType);
                    result(status, headers, body);
                },
                fault);
        }
    }
}