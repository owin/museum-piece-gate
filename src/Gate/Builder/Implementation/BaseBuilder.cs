using System;
using System.Collections.Generic;
using System.Linq;
using Gate.Owin;

namespace Gate.Builder.Implementation
{
    class BaseBuilder : IAppBuilder
    {
        readonly IList<Func<AppDelegate, AppDelegate>> _stack;

        public BaseBuilder()
        {
            _stack = new List<Func<AppDelegate, AppDelegate>>();
        }

        public IAppBuilder Use(Func<AppDelegate, AppDelegate> factory)
        {
            _stack.Add(factory);
            return this;
        }

        public AppDelegate Build()
        {
            return _stack
                .Reverse()
                .Aggregate(NotFound.App(), (app, factory) => factory(app));
        }

    }
}
