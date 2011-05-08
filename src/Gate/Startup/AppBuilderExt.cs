using System;
using System.Collections.Generic;

namespace Gate.Startup
{
    using AppAction = Action< // app
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

    /// <summary>
    /// Variant of the AppBuilder to streamline support for Action/Func only OWIN middleware and application endpoints
    /// </summary>
    public class AppBuilderExt
    {
        readonly AppBuilder _builder;

        public AppBuilderExt(AppBuilder builder)
        {
            _builder = builder;
        }

        public AppBuilder Use(Func<AppAction, AppAction> factory)
        {
            return _builder.Use(app => factory(app.ToAction()).ToDelegate());
        }

        public AppBuilder Run(Func<AppAction> factory)
        {
            return _builder.Run(() => factory().ToDelegate());
        }

        public AppBuilder Map(string path, Action<AppBuilderExt> configuration)
        {
            return _builder.Map(path, map => configuration(map.Ext));
        }
    }
}