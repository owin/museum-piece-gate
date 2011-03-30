using System;
using System.Collections.Generic;

namespace Gate.Startup
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

    public class AppBuilder
    {
        AppDelegate _app;

        public AppBuilder Run(Func<AppDelegate> factory)
        {
            _app = factory();
            return this;
        }

        public AppDelegate Build()
        {
            return _app;
        }
    }
}