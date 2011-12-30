using System;
using System.Collections.Generic;
using Gate.Owin;
using Kayak;

namespace Gate.Hosts.Kayak
{
    public class RequestEnvironment
    {
        public const string RequestMethodKey = OwinConstants.RequestMethod;
        public const string RequestPathBaseKey = OwinConstants.RequestPathBase;
        public const string RequestPathKey = OwinConstants.RequestPath;
        public const string RequestQueryStringKey = OwinConstants.RequestQueryString;
        public const string RequestBodyKey = OwinConstants.RequestBody;
        public const string RequestHeadersKey = OwinConstants.RequestHeaders;
        public const string RequestSchemeKey = OwinConstants.RequestScheme;
        public const string VersionKey = OwinConstants.Version;
        public const string SchedulerKey = "kayak.Scheduler";

        readonly IDictionary<string, object> _env;

        public RequestEnvironment(IDictionary<string, object> env)
        {
            _env = env;
        }

        T Get<T>(string name)
        {
            object value;
            return _env.TryGetValue(name, out value) ? (T)value : default(T);
        }


        public string Version
        {
            get { return Get<string>(VersionKey); }
            set { _env[VersionKey] = value; }
        }

        public string Method
        {
            get { return Get<string>(RequestMethodKey); }
            set { _env[RequestMethodKey] = value; }
        }

        public IDictionary<string, string> Headers
        {
            get { return Get<IDictionary<string, string>>(RequestHeadersKey); }
            set { _env[RequestHeadersKey] = value; }
        }

        public string PathBase
        {
            get { return Get<string>(RequestPathBaseKey); }
            set { _env[RequestPathBaseKey] = value; }
        }

        public string Path
        {
            get { return Get<string>(RequestPathKey); }
            set { _env[RequestPathKey] = value; }
        }

        public string Scheme
        {
            get { return Get<string>(RequestSchemeKey); }
            set { _env[RequestSchemeKey] = value; }
        }

        public string QueryString
        {
            get { return Get<string>(RequestQueryStringKey); }
            set { _env[RequestQueryStringKey] = value; }
        }

        public BodyDelegate BodyDelegate
        {
            get { return Get<BodyDelegate>(RequestBodyKey); }
            set { _env[RequestBodyKey] = value; }
        }

        public IScheduler Scheduler
        {
            get { return Get<IScheduler>(SchedulerKey); }
            set { _env[SchedulerKey] = value; }
        }
    }
}
