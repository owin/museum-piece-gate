using System.Collections.Generic;
using Gate.Helpers.Utils;

namespace Gate.Helpers
{
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