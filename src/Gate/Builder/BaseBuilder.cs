using System;
using System.Collections.Generic;
using System.Linq;
using Gate.Owin;

namespace Gate.Builder
{
    class BaseBuilder : IAppBuilder
    {
        readonly IList<Func<AppDelegate, AppDelegate>> _stack;

        public BaseBuilder()
        {
            _stack = new List<Func<AppDelegate, AppDelegate>>();
        }

        public IAppBuilder Use(Func<AppDelegate, AppDelegate> middleware)
        {
            _stack.Add(middleware);
            return this;
        }

        public AppDelegate Fork(Action<IAppBuilder> fork)
        {
            var b = new BaseBuilder();
            fork(b);
            return b.Build();
        }

        public AppDelegate Build()
        {
            return _stack
                .Reverse()
                .Aggregate(NotFound.App(), (app, factory) => factory(app));
        }

    }
}
