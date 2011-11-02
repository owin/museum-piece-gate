using System;
using System.Collections.Generic;
using Gate.Builder.Loader;
using Gate.Owin;
using System.Linq;

namespace Gate.Builder
{
    public class AppBuilder : IAppBuilder
    {
        public static AppDelegate BuildConfiguration()
        {
            return BuildConfiguration(default(string));
        }

        public static AppDelegate BuildConfiguration(string startupName)
        {
            var startup = new StartupLoader().Load(startupName);
            return BuildConfiguration(startup);
        }

        public static AppDelegate BuildConfiguration(Action<IAppBuilder> startup)
        {
            if (startup == null)
                throw new ArgumentNullException("startup");

            var builder = new AppBuilder();
            startup(builder);
            return builder.Materialize();
        }

        readonly IList<Func<AppDelegate, AppDelegate>> _stack;

        public AppBuilder()
        {
            _stack = new List<Func<AppDelegate, AppDelegate>>();
        }

        public IAppBuilder Use(Func<AppDelegate, AppDelegate> middleware)
        {
            _stack.Add(middleware);
            return this;
        }

        public AppDelegate Build(Action<IAppBuilder> fork)
        {
            var b = new AppBuilder();
            fork(b);
            return b.Materialize();
        }

        public AppDelegate Materialize()
        {
            return _stack
                .Reverse()
                .Aggregate(NotFound.App(), (app, factory) => factory(app));
        }
    }
}