using System;
using System.Collections.Generic;
using System.Linq;
using Gate.Startup.Loader;

namespace Gate.Startup
{
    public class AppBuilder
    {
        public static AppDelegate BuildFromConfiguration(string configurationString)
        {
            var builder = new AppBuilder();
            new DefaultConfigurationLoader().Load(configurationString)(builder);
            return builder.Build();
        }

        public IConfigurationLoader ConfigurationLoader { get; set; }
        readonly IList<Func<AppDelegate, AppDelegate>> _stack = new List<Func<AppDelegate, AppDelegate>>();

        Func<AppDelegate, IDictionary<string, AppDelegate>, AppDelegate> _mapper = UrlMapper.Create;
        IDictionary<string, AppDelegate> _maps;


        public AppBuilder() : this(UrlMapper.Create) { }

        public AppBuilder(
            Func<AppDelegate, IDictionary<string, AppDelegate>, AppDelegate> mapper)
        {
            _mapper = mapper;
        }

        public AppDelegate Branch(Action<AppBuilder> configuration)
        {
            var builder = new AppBuilder(_mapper);
            configuration(builder);
            return builder.Build();
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
            _maps[path] = Branch(configuration);
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