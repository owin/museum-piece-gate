using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Startup
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
                .Aggregate(NotFound.Create(), (app, factory) => factory(app));
        }
    }
}
