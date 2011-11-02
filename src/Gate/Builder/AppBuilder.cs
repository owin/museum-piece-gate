using System;
using Gate.Builder.Loader;
using Gate.Owin;

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
            return builder.Build();
        }

        readonly BaseBuilder _builder;

        public AppBuilder()
        {
            _builder = new BaseBuilder();
        }

        public IAppBuilder Use(Func<AppDelegate, AppDelegate> middleware)
        {
            return _builder.Use(middleware);
        }

        public AppDelegate Fork(Action<IAppBuilder> fork)
        {
            return _builder.Fork(fork);
        }

        public AppDelegate Build()
        {
            return _builder.Build();
        }
    }
}