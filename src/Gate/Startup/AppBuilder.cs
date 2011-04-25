using System;
using System.Collections.Generic;
using System.Linq;
using Gate.Startup.Loader;

namespace Gate.Startup
{
    public class AppBuilder
    {
        public IConfigurationLoader ConfigurationLoader { get; set; }
        readonly IList<Func<AppDelegate, AppDelegate>> _stack = new List<Func<AppDelegate, AppDelegate>>();

        Func<AppDelegate, IDictionary<string, AppDelegate>, AppDelegate> _mapper = UrlMapper.Create;
        IDictionary<string, AppDelegate> _maps;

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

        public AppBuilder SetUrlMapper(Func<AppDelegate, IDictionary<string, AppDelegate>, AppDelegate> mapper)
        {
            _mapper = mapper;
            return this;
        }

        public AppBuilder Configure()
        {
            return Configure(default(string));
        }

        public AppBuilder Configure(Action<AppBuilder> configuration)
        {
            configuration(this);
            return this;
        }

        public AppBuilder Configure(string configurationString)
        {
            var configuration = ConfigurationLoader.Load(configurationString);
            if (configuration == null)
                throw new ArgumentException("Configuration not loadable", "configurationString");
            return Configure(configuration);
        }

        public AppBuilderExt Ext
        {
            get {return new AppBuilderExt(this);}
        }

        public AppBuilder Use(Func<AppDelegate, AppDelegate> factory)
        {
            _stack.Add(factory);
            _maps = null;
            return this;
        }

        public AppBuilder Run(Func<AppDelegate> factory)
        {
            _stack.Add(_ => factory());
            _maps = null;
            return this;
        }

        public AppBuilder Map(string path, Action<AppBuilder> configuration)
        {
            if (_maps == null)
            {
                var maps = new Dictionary<string, AppDelegate>();
                _stack.Add(app => _mapper(app, maps));
                _maps = maps;
            }
            _maps[path] = new AppBuilder(ConfigurationLoader)
                .SetUrlMapper(_mapper)
                .Configure(configuration)
                .Build();
            return this;
        }

        public AppDelegate Build()
        {
            return _stack
                .Reverse()
                .Aggregate(NotFound.Create(), (app, factory) => factory(app));
        }
    }
}