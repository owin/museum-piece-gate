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
        readonly IAppBuilder _builder;

        public AppBuilderExt(IAppBuilder builder)
        {
            _builder = builder;
        }

        public IAppBuilder Use(Func<AppAction, AppAction> factory)
        {
            return _builder.Use(app => factory(app.ToAction()).ToDelegate());
        }

        public IAppBuilder Run(Func<AppAction> factory)
        {
            return _builder.Run(() => factory().ToDelegate());
        }

        public IAppBuilder Map(string path, Action<AppBuilderExt> configuration)
        {
            return _builder.Map(path, (IAppBuilder map) => configuration(map.GetExt()));
        }
    }

    public static class AppBuilderExtExtensions
    {
        public static AppBuilderExt GetExt(this IAppBuilder builder)
        {
            return new AppBuilderExt(builder);
        }
    }
}