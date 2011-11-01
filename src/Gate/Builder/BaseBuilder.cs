using System;
using System.Collections.Generic;
using System.Linq;
using Gate.Owin;

namespace Gate.Builder
{
    class BaseBuilder : IAppBuilder
    {
        readonly IList<Func<AppDelegate, AppDelegate>> _stack;
        readonly Func<Action<IAppBuilder>, AppDelegate> _forkMethod;

        public BaseBuilder(Func<Action<IAppBuilder>, AppDelegate> forkMethod)
        {
            _stack = new List<Func<AppDelegate, AppDelegate>>();
            _forkMethod = forkMethod;
        }

        public IAppBuilder Use(Func<AppDelegate, AppDelegate> middleware)
        {
            _stack.Add(middleware);
            return this;
        }

        public AppDelegate Fork(Action<IAppBuilder> fork)
        {
            return _forkMethod(fork);
        }


        public AppDelegate Build()
        {
            return _stack
                .Reverse()
                .Aggregate(NotFound.App(), (app, factory) => factory(app));
        }

    }
}
