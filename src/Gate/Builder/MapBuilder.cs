using System;
using System.Collections.Generic;
using Gate.Owin;

namespace Gate.Builder
{
    class MapBuilder : IAppBuilder
    {
        readonly IAppBuilder _builder;
        readonly IDictionary<string, AppDelegate> _map;
        readonly Func<AppDelegate, IDictionary<string, AppDelegate>, AppDelegate> _mapper;

        public MapBuilder(IAppBuilder builder, Func<AppDelegate, IDictionary<string, AppDelegate>, AppDelegate> mapper)
        {
            _map = new Dictionary<string, AppDelegate>();
            _mapper = mapper;
            _builder = builder.Use(a => _mapper(a, _map));
        }

        public void MapInternal(string path, AppDelegate app)
        {
            _map[path] = app;
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
