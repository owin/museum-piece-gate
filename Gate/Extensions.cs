using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate
{
    public static class Extensions
    {
        public static string GetRequestMethod(this IDictionary<string, object> env)
        {
            return env["owin.RequestMethod"] as string;
        }

        public static void SetRequestMethod(this IDictionary<string, object> env, string value)
        {
            env["owin.RequestMethod"] = value;
        }

        public static string GetRequestUri(this IDictionary<string, object> env)
        {
            return env["owin.RequestUri"] as string;
        }

        public static void SetRequestUri(this IDictionary<string, object> env, string value)
        {
            env["owin.RequestUri"] = value;
        }

        public static IDictionary<string, string> GetRequestHeaders(this IDictionary<string, object> env)
        {
            return env["owin.RequestHeaders"] as IDictionary<string, string>;
        }

        public static void SetRequestHeaders(this IDictionary<string, object> env, IDictionary<string, string> value)
        {
            env["owin.RequestHeaders"] = value;
        }

        public static string GetRequestHeader(this IDictionary<string, object> env, string name)
        {
            var headers = env.GetRequestHeaders();
            return headers.ContainsKey(name) ? headers[name] : null;
        }

        public static Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>
            GetRequestBody(this IDictionary<string, object> env)
        {
            return
                env["gate.RequestBody"] as
                Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>;
        }

        public static void SetRequestBody(this IDictionary<string, object> env,
                                          Func
                                              <Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action
                                              > value)
        {
            env["gate.RequestBody"] = value;
        }
    }
}