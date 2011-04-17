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
    /// defined by the OWIN spec.
    /// </summary>
    public class Owin
    {
        public static readonly string RequestMethodKey = "owin.RequestMethod";
        public static readonly string RequestPathBaseKey = "owin.RequestPathBase";
        public static readonly string RequestPathKey = "owin.RequestPath";
        public static readonly string RequestQueryStringKey = "owin.RequestQueryString";
        public static readonly string RequestBodyKey = "owin.RequestBody";
        public static readonly string RequestHeadersKey = "owin.RequestHeaders";
        public static readonly string RequestSchemeKey = "owin.RequestScheme";
        public static readonly string VersionKey = "owin.Version";

        protected readonly IDictionary<string, object> Env;


        protected T Get<T>(string name)
        {
            object value;
            return Env.TryGetValue(name, out value) ? (T) value : default(T);
        }

        public Owin(IDictionary<string, object> env)
        {
            Env = env;
        }

        /// <summary>
        /// "owin.Version" The string "1.0" indicating OWIN version 1.0. 
        /// </summary>
        public string Version
        {
            get { return Get<string>(VersionKey); }
            set { Env[VersionKey] = value; }
        }

        /// <summary>
        /// "owin.RequestMethod" A string containing the HTTP request method of the request (e.g., "GET", "POST"). 
        /// </summary>
        public string Method
        {
            get { return Get<string>(RequestMethodKey); }
            set { Env[RequestMethodKey] = value; }
        }

        /// <summary>
        /// "owin.RequestHeaders" An instance of IDictionary&lt;string, string&gt; which represents the HTTP headers present in the request (the request header dictionary).
        /// </summary>
        public IDictionary<string, string> Headers
        {
            get { return Get<IDictionary<string, string>>(RequestHeadersKey); }
            set { Env[RequestHeadersKey] = value; }
        }

        /// <summary>
        /// "owin.RequestPathBase" A string containing the portion of the request path corresponding to the "root" of the application delegate. The value may be an empty string.  
        /// </summary>
        public string PathBase
        {
            get { return Get<string>(RequestPathBaseKey); }
            set { Env[RequestPathBaseKey] = value; }
        }

        /// <summary>
        /// "owin.RequestPath" A string containing the request path. The path must be relative to the "root" of the application delegate. 
        /// </summary>
        public string Path
        {
            get { return Get<string>(RequestPathKey); }
            set { Env[RequestPathKey] = value; }
        }

        /// <summary>
        /// "owin.RequestScheme" A string containing the URI scheme used for the request (e.g., "http", "https").  
        /// </summary>
        public string Scheme
        {
            get { return Get<string>(RequestSchemeKey); }
            set { Env[RequestSchemeKey] = value; }
        }

        /// <summary>
        /// "owin.RequestBody" An instance of the body delegate representing the body of the request. May be null.
        /// </summary>
        public BodyDelegate Body
        {
            get { return Get<BodyDelegate>(RequestBodyKey); }
            set { Env[RequestBodyKey] = value; }
        }

        /// <summary>
        /// "owin.QueryString" A string containing the query string component of the HTTP request URI (e.g., "foo=bar&baz=quux"). The value may be an empty string.
        /// </summary>
        public string QueryString
        {
            get { return Get<string>(RequestQueryStringKey); }
            set { Env[RequestQueryStringKey] = value; }
        }
    }
}