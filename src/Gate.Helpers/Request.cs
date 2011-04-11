using System;
using System.Collections.Generic;
using System.Net;
using Gate.Helpers.Utils;

namespace Gate.Helpers
{
    using BodyDelegate = Func<
        // on next
        Func<
            ArraySegment<byte>, // data
            Action, // continuation
            bool // continuation was or will be invoked
            >,
        // on error
        Action<Exception>,
        // on complete
        Action,
        // cancel 
        Action
        >;

    public class Request : Owin
    {
        public Request(IDictionary<string, object> env) : base(env)
        {
        }

        public IDictionary<string, string> Query
        {
            get
            {
                var text = QueryString;
                if (Get<string>("Gate.Helpers.Request.Query:text") != text ||
                    Get<IDictionary<string, string>>("Gate.Helpers.Request.Query") == null)
                {
                    Env["Gate.Helpers.Request.Query:text"] = text;
                    Env["Gate.Helpers.Request.Query"] = ParamDictionary.Parse(text);
                }
                return Get<IDictionary<string, string>>("Gate.Helpers.Request.Query");
            }
        }
    }
}