using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Utility class providing strongly-typed get/set access to environment properties 
    /// defined by the OWIN spec
    /// </summary>
    public class Environment
    {
        readonly IDictionary<string, object> _env;

        public Environment(IDictionary<string, object> env)
        {
            _env = env;
        }

        T Get<T>(string name)
        {
            object value;
            return _env.TryGetValue(name, out value) ? (T) value : default(T);
        }

        void Set<T>(string name, T value)
        {
            _env[name] = value;
        }

        /// <summary>
        /// "owin.Version" 	The string "1.0" indicating OWIN version 1.0. 
        /// </summary>
        public string Version
        {
            get { return Get<string>("owin.Version"); }
            set { Set("owin.Version", value); }
        }

        /// <summary>
        /// "owin.RequestMethod" 	A string containing the HTTP request method of the request (e.g., "GET", "POST").
        /// </summary>
        public string Method
        {
            get { return Get<string>("owin.RequestMethod"); }
            set { Set("owin.RequestMethod", value); }
        }

        /// <summary>
        /// "owin.RequestHeaders" 	An instance of IDictionary&lt;string, string&gt; which represents the HTTP headers present in the request (the request header dictionary); see Headers.
        /// </summary>
        public IDictionary<string, string> Headers
        {
            get { return Get<IDictionary<string, string>>("owin.RequestHeaders"); }
            set { Set("owin.RequestHeaders", value); }
        }


        /// <summary>
        /// A string containing the request path. The path must be relative to the "root" of the application delegate
        /// </summary>
        public string RequestPath
        {
            get { return Get<string>("owin.RequestPath"); }
            set { Set("owin.RequestPath", value); }
        }

        /// <summary>
        /// "owin.RequestPathBase" 	A string containing the portion of the request URI's path corresponding to the "root" of the application object. See Paths.
        /// </summary>
        public string RequestPathBase
        {
            get { return Get<string>("owin.RequestPathBase"); }
            set { Set("owin.RequestPathBase", value); }
        }

        /// <summary>
        /// "owin.QueryString"  A string containing the query string component of the HTTP request URI (e.g., "foo=bar&baz=quux"). The value may be an empty string.
        /// </summary>
        public string QueryString
        {
            get { return Get<string>("owin.QueryString"); }
            set { Set("owin.QueryString", value); }
        }

        /// <summary>
        /// "owin.RequestScheme" 	A string representing the URI scheme (e.g. "http", "https")
        /// </summary>
        public string RequestScheme
        {
            get { return Get<string>("owin.RequestScheme"); }
            set { Set("owin.RequestScheme", value); }
        }

        /// <summary>
        /// "owin.RequestBody" 	An instance of the body delegate representing the body of the request. May be null.
        /// </summary>
        public BodyDelegate Body
        {
            get { return Get<BodyDelegate>("owin.RequestBody"); }
            set { Set("owin.RequestBody", value); }
        }

        /// <summary>
        /// "owin.ServerName"	Hosts should provide values which can be used to reconstruct the full URI of the request in absence of the HTTP Host header of the request.
        /// </summary>
        public string ServerName
        {
            get { return Get<string>("owin.ServerName"); }
            set { Set("owin.ServerName", value); }
        }

        /// <summary>
        /// "owin.ServerPort" 	Hosts should provide values which can be used to reconstruct the full URI of the request in absence of the HTTP Host header of the request.
        /// </summary>
        public string ServerPort
        {
            get { return Get<string>("owin.ServerPort"); }
            set { Set("owin.ServerPort", value); }
        }

        /// <summary>
        /// "owin.RemoteEndPoint" 	A System.Net.IPEndPoint representing the connected client.
        /// </summary>
        public System.Net.IPEndPoint RemoteEndPoint
        {
            get { return Get<System.Net.IPEndPoint>("owin.RemoteEndPoint"); }
            set { Set("owin.RemoteEndPoint", value); }
        }
    }
}