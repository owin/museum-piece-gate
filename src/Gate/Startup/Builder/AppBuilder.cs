﻿using System;

namespace Gate
{
    public class AppBuilder : IAppBuilder
    {
        public static AppDelegate BuildConfiguration(string configurationString)
        {
            var configuration = new DefaultConfigurationLoader().Load(configurationString);
            return BuildConfiguration(configuration);
        }

        public static AppDelegate BuildConfiguration(Action<IAppBuilder> configuration)
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