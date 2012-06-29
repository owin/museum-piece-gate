using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Owin;

namespace Gate.Mapping
{
    public class UrlMapper
    {
        readonly AppDelegate _app;
        IEnumerable<Tuple<string, AppDelegate>> _map = Enumerable.Empty<Tuple<string, AppDelegate>>();

        UrlMapper(AppDelegate app)
        {
            _app = app;
        }


        public static AppDelegate Create(AppDelegate app, IDictionary<string, AppDelegate> map)
        {
            if (app == null)
                throw new ArgumentNullException("app");

            var mapper = new UrlMapper(app);
            mapper.Remap(map);
            return mapper.Call;
        }

        public void Remap(IDictionary<string, AppDelegate> map)
        {
            _map = map
                .Select(kv => Tuple.Create(kv.Key, kv.Value))
                .OrderByDescending(m => m.Item1.Length)
                .ToArray();
        }

        public Task<ResultParameters> Call(CallParameters call)
        {
            var paths = new Paths(call.Environment);
            var path = paths.Path;
            var pathBase = paths.PathBase;
            
            var match = _map.FirstOrDefault(m => path.StartsWith(m.Item1));
            if (match == null)
            {
                // fall-through to default
                return _app(call);
            }

            // Map moves the matched portion of Path into PathBase
            paths.PathBase = pathBase + match.Item1;
            paths.Path = path.Substring(match.Item1.Length);
            return match.Item2.Invoke(call).Then(result =>
            {
                // Path and PathBase are restored as the call returnss
                paths.Path = path;
                paths.PathBase = pathBase;
                return result;
            });
        }

        /// <summary>
        /// This is a very small version of Environment, repeated here so the Gate.Builder.dll
        /// doesn't need to take a hard dependency on Gate.dll
        /// </summary>
        class Paths
        {
            readonly IDictionary<string, object> _env;

            public Paths(IDictionary<string, object> env)
            {
                _env = env;
            }

            const string RequestPathBaseKey = OwinConstants.RequestPathBase;
            const string RequestPathKey = OwinConstants.RequestPath;

            public string Path
            {
                get
                {
                    object value;
                    return _env.TryGetValue(RequestPathKey, out value) ? Convert.ToString(value) : null;
                }
                set
                {
                    _env[RequestPathKey] = value;
                }
            }

            public string PathBase
            {
                get
                {
                    object value;
                    return _env.TryGetValue(RequestPathBaseKey, out value) ? Convert.ToString(value) : null;
                }
                set
                {
                    _env[RequestPathBaseKey] = value;
                }
            }
        }
    }
}
