using System;
using System.Collections.Generic;
using System.Linq;

namespace Gate.TestHelpers
{
    public class FakeHostRequest : Environment
    {
        public FakeHostRequest() : base() { }
        public FakeHostRequest(IDictionary<string, object> env) : base(env)
        {
        }

        public static Action<FakeHostRequest> GetRequest()
        {
            return GetRequest("/");
        }

        public static Action<FakeHostRequest> GetRequest(string path)
        {
            return GetRequest(path, r => { });
        }

        public static Action<FakeHostRequest> GetRequest(string path, Action<FakeHostRequest> requestSetup)
        {
            var pathParts = path.Split("?".ToArray(), 2);
            return request =>
            {
                request.Method = "GET";
                request.PathBase = "";
                request.Path = pathParts[0];
                request.QueryString = pathParts.Length == 2 ? pathParts[1] : null;
                requestSetup(request);
            };
        }
    }
}