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

    public class UrlMapper
    {
        readonly AppDelegate _app;

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
            //TODO
        }

        public void Call(IDictionary<string, object> env,
            Action<string, IDictionary<string, string>, Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>> result,
            Action<Exception> fault)
        {
            _app(env, result, fault);
        }
    }
}
