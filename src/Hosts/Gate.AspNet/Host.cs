using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.AspNet
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


    public static class Host
    {
        static AppDelegate _app = (env, result, fault) => result("404 NOTFOUND", null, null);

        public static void Run(AppDelegate app)
        {
            _app = app;
        }

        public static AppDelegate Call
        {
            get { return _app; }
        }
    }
}