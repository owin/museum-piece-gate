using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate
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

    public static class Extensions
    {
        static T Get<T>(IDictionary<string, object> env, string name)
        {
            object value;
            return env.TryGetValue(name, out value) ? (T)value : default(T);
        }

        public static string GetRequestMethod(this IDictionary<string, object> env)
        {
            return Get<string>(env, "owin.RequestMethod");
        }

        public static void SetRequestMethod(this IDictionary<string, object> env, string value)
        {
            env["owin.RequestMethod"] = value;
        }

        public static string GetRequestPath(this IDictionary<string, object> env)
        {
            return Get<string>(env, "owin.RequestPath");
        }

        public static void SetRequestPath(this IDictionary<string, object> env, string value)
        {
            env["owin.RequestPath"] = value;
        }

        public static string GetRequestPathBase(this IDictionary<string, object> env)
        {
            return Get<string>(env, "owin.RequestPathBase");
        }

        public static void SetRequestPathBase(this IDictionary<string, object> env, string value)
        {
            env["owin.RequestPathBase"] = value;
        }

        public static string GetRequestQueryString(this IDictionary<string, object> env)
        {
            return Get<string>(env, "owin.RequestQueryString");
        }

        public static void SetRequestQueryString(this IDictionary<string, object> env, string value)
        {
            env["owin.RequestQueryString"] = value;
        }

        public static IDictionary<string, string> GetRequestHeaders(this IDictionary<string, object> env)
        {
            return Get<IDictionary<string, string>>(env, "owin.RequestHeaders");
        }

        public static void SetRequestHeaders(this IDictionary<string, object> env, IDictionary<string, string> value)
        {
            env["owin.RequestHeaders"] = value;
        }

        public static string GetRequestHeader(this IDictionary<string, object> env, string name)
        {
            var headers = env.GetRequestHeaders();
            string value;
            return headers.TryGetValue(name, out value) ? value : default(string);
        }

        public static BodyDelegate GetRequestBody(this IDictionary<string, object> env)
        {
            return Get<BodyDelegate>(env, "gate.RequestBody");
        }

        public static void SetRequestBody(this IDictionary<string, object> env, BodyDelegate value)
        {
            env["gate.RequestBody"] = value;
        }

        public static string GetRequestScheme(this IDictionary<string, object> env)
        {
            return Get<string>(env, "owin.RequestScheme");
        }

        public static void SetRequestScheme(this IDictionary<string, object> env, string value)
        {
            env["owin.RequestScheme"] = value;
        }

        public static string GetVersion(this IDictionary<string, object> env)
        {
            return Get<string>(env, "owin.Version");
        }

        public static void SetVersion(this IDictionary<string, object> env, string value)
        {
            env["owin.Version"] = value;
        }

    }
}