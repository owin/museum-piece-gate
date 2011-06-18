using System;
using System.Collections.Generic;
using System.Linq;
using Gate.Startup.Loader;

namespace Gate.Startup
{
    public class AppBuilder : IAppBuilder
    {
        public static AppDelegate BuildFromConfiguration(string configurationString)
        {
            var configuration = new DefaultConfigurationLoader().Load(configurationString);
            return BuildFromConfiguration(configuration);
        }

        public static AppDelegate BuildFromConfiguration(Action<IAppBuilder> configuration)
        {
            var builder = new AppBuilder();
            configuration(builder);
            return builder.Build();
        }

        readonly BaseBuilder _builder;

        public AppBuilder()
        {
            _builder = new BaseBuilder();
        }

        public IAppBuilder Use(Func<AppDelegate, AppDelegate> factory)
        {
            return _builder.Use(factory);
        }

        public AppDelegate Build()
        {
            return _builder.Build();
        }
    }
}