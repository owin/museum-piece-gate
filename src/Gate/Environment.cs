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

        /// <summary>
        /// "owin.Version" The string "1.0" indicating OWIN version 1.0. 
        /// </summary>
        public string Version
        {
            get { return _env.GetVersion(); }
            set { _env.SetVersion(value); }
        }

        /// <summary>
        /// "owin.RequestMethod" A string containing the HTTP request method of the request (e.g., "GET", "POST"). 
        /// </summary>
        public string Method
        {
            get { return _env.GetRequestMethod(); }
            set { _env.SetRequestMethod(value); }
        }

        /// <summary>
        /// "owin.RequestHeaders" An instance of IDictionary&lt;string, string&gt; which represents the HTTP headers present in the request (the request header dictionary).
        /// </summary>
        public IDictionary<string, string> Headers
        {
            get { return _env.GetRequestHeaders(); }
            set { _env.SetRequestHeaders(value); }
        }

        /// <summary>
        /// "owin.RequestPathBase" A string containing the portion of the request path corresponding to the "root" of the application delegate. The value may be an empty string.  
        /// </summary>
        public string PathBase
        {
            get { return _env.GetRequestPathBase(); }
            set { _env.SetRequestPathBase(value); }
        }

        /// <summary>
        /// "owin.RequestPath" A string containing the request path. The path must be relative to the "root" of the application delegate. 
        /// </summary>
        public string Path
        {
            get { return _env.GetRequestPath(); }
            set { _env.SetRequestPath(value); }
        }

        /// <summary>
        /// "owin.RequestScheme" A string containing the URI scheme used for the request (e.g., "http", "https").  
        /// </summary>
        public string Scheme
        {
            get { return _env.GetRequestScheme(); }
            set { _env.SetRequestScheme(value); }
        }

        /// <summary>
        /// "owin.RequestBody" An instance of the body delegate representing the body of the request. May be null.
        /// </summary>
        public BodyDelegate Body
        {
            get { return _env.GetRequestBody(); }
            set { _env.SetRequestBody(value); }
        }

        
        /// <summary>
        /// "owin.QueryString" A string containing the query string component of the HTTP request URI (e.g., "foo=bar&baz=quux"). The value may be an empty string.
        /// </summary>
        public string QueryString
        {
            get { return _env.GetRequestQueryString(); }
            set { _env.SetRequestQueryString(value); }
        }
    }
}