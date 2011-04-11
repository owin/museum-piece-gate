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

    public class Request : Environment
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
                    _env["Gate.Helpers.Request.Query:text"] = text;
                    _env["Gate.Helpers.Request.Query"] = ParamDictionary.Parse(text);
                }
                return Get<IDictionary<string, string>>("Gate.Helpers.Request.Query");
            }
        }
    }
}