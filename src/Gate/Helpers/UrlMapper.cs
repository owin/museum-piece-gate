using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Utils;

namespace Gate.Helpers
{
    using AppDelegate = Action< // app
        IDictionary<string, object>, // env
        Action< // result
            string, // status
            IDictionary<string, string>, // headers
            Func< // body
                Func< // next
                    ArraySegment<byte>, // data
                    Action, // continuation
                    bool>, // async                    
                Action<Exception>, // error
                Action, // complete
                Action>>, // cancel
        Action<Exception>>; // error
    using ResultDelegate = Action< // result
        string, // status
        IDictionary<string, string>, // headers
        Func< // body
            Func< // next
                ArraySegment<byte>, // data
                Action, // continuation
                bool>, // async                    
            Action<Exception>, // error
            Action, // complete
            Action>>; // cancel

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
            var owin = new Owin(env);
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