using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Utils;

namespace Gate
{
    public class UrlMapper
    {
        readonly AppDelegate _app;
        IEnumerable<Tuple<string, AppDelegate>> _map = Enumerable.Empty<Tuple<string, AppDelegate>>();

        UrlMapper(AppDelegate app)
        {
            _app = app;
        }

        public static AppDelegate Create(IDictionary<string, AppDelegate> map)
        {
            return Create(null, map);
        }

        public static AppDelegate Create(AppDelegate app, IDictionary<string, AppDelegate> map)
        {
            var mapper = new UrlMapper(app ?? NotFound.Create());
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
            IDictionary<string, object> env,
            ResultDelegate result,
            Action<Exception> fault)
        {
            var owin = new Environment(env);
            var path = owin.Path;
            var pathBase = owin.PathBase;
            Action finish = () =>
            {
                owin.Path = path;
                owin.PathBase = pathBase;
            };
            var match = _map.FirstOrDefault(m => path.StartsWith(m.Item1));
            if (match == null)
            {
                // fall-through to default
                _app(env, result, fault);
                return;
            }
            owin.PathBase = pathBase + match.Item1;
            owin.Path = path.Substring(match.Item1.Length);
            match.Item2.Invoke(env, result, fault);
        }
    }
}