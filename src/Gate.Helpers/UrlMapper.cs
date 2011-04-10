using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static AppDelegate New(IDictionary<string, AppDelegate> map)
        {
            return New(null, map);
        }

        public static AppDelegate New(AppDelegate app, IDictionary<string, AppDelegate> map)
        {
            var mapper = new UrlMapper(app ?? NotFound.New());
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
            var request = new Request(env);
            var path = request.Path;
            var pathBase = request.PathBase;
            Action finish = () =>
            {
                request.Path = path;
                request.PathBase = pathBase;
            };
            var match = _map.FirstOrDefault(m => path.StartsWith(m.Item1));
            if (match == null)
            {
                // fall-through to default
                _app(env, result, fault);
                return;
            }
            request.PathBase = pathBase + match.Item1;
            request.Path = path.Substring(match.Item1.Length);
            match.Item2.Invoke(env, result, fault);
        }
    }
}