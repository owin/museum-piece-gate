using System;
using System.Collections.Generic;
using Gate.Startup.Loader;

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
        public IConfigurationLoader ConfigurationLoader { get; set; }
        AppDelegate _app;
        readonly Stack<Func<AppDelegate, AppDelegate>> _middleware = new Stack<Func<AppDelegate, AppDelegate>>();

        public AppBuilder()
            : this(new DefaultConfigurationLoader())
        {
        }

        public AppBuilder(string configurationString)
            : this(new DefaultConfigurationLoader(), configurationString)
        {
        }

        public AppBuilder(Action<AppBuilder> configuration)
            : this(new DefaultConfigurationLoader(), configuration)
        {
        }

        public AppBuilder(IConfigurationLoader configurationLoader)
        {
            ConfigurationLoader = configurationLoader;
        }

        public AppBuilder(IConfigurationLoader configurationLoader, string configurationString)
            : this(configurationLoader)
        {
            Configure(configurationString);
        }

        public AppBuilder(IConfigurationLoader configurationLoader, Action<AppBuilder> configuration)
            : this(configurationLoader)
        {
            Configure(configuration);
        }


        public AppBuilder Configure(Action<AppBuilder> configuration)
        {
            configuration(this);
            return this;
        }

        public AppBuilder Configure(string configurationString)
        {
            return Configure(ConfigurationLoader.Load(configurationString));
        }

        public AppBuilder Use(Func<AppDelegate, AppDelegate> factory)
        {
            _middleware.Push(factory);
            return this;
        }

        public AppBuilder Run(Func<AppDelegate> factory)
        {
            var chain = factory.Invoke();
            while (_middleware.Count != 0)
            {
                chain = _middleware.Pop().Invoke(chain);
            }
            _app = chain;
            return this;
        }

        public AppDelegate Build()
        {
            return _app;
        }
    }
}