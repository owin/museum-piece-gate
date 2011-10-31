using System;
using System.Collections.Generic;
using System.Linq;

namespace Gate
{
    class UrlMapper
    {
        readonly AppDelegate _app;
        IEnumerable<Tuple<string, AppDelegate>> _map = Enumerable.Empty<Tuple<string, AppDelegate>>();

        UrlMapper(AppDelegate app)
        {
            _app = app;
        }

        public static AppDelegate Create(IDictionary<string, AppDelegate> map)
        {
            return Create(NotFound.App(), map);
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

        public void Call(
            IDictionary<string, object> envDict,
            ResultDelegate result,
            Action<Exception> fault)
        {
            var env = envDict as Environment ?? new Environment(envDict);
            var path = env.Path;
            var pathBase = env.PathBase;
            Action finish = () =>
            {
                env.Path = path;
                env.PathBase = pathBase;
            };
            var match = _map.FirstOrDefault(m => path.StartsWith(m.Item1));
            if (match == null)
            {
                // fall-through to default
                _app(env, result, fault);
                return;
            }
            env.PathBase = pathBase + match.Item1;
            env.Path = path.Substring(match.Item1.Length);
            match.Item2.Invoke(env, result, fault);
        }
    }
}
