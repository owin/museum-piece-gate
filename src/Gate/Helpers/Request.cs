using System;
using System.Collections.Generic;
using Gate.Helpers.Utils;

namespace Gate
{
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
                    Env["Gate.Helpers.Request.Query:text"] = text;
                    Env["Gate.Helpers.Request.Query"] = ParamDictionary.Parse(text);
                }
                return Get<IDictionary<string, string>>("Gate.Helpers.Request.Query");
            }
        }

        public string Host  
        {
            get { return "Host"; }            
        }
    }
}