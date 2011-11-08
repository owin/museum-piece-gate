using System;
using System.Collections.Generic;
using Gate.Builder.Loader;
using Gate.Owin;
using System.Linq;

namespace Gate.Builder
{
    using AppAction = Action< // app
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

    public class AppBuilder : IAppBuilder
    {
        public static AppDelegate BuildConfiguration()
        {
            return BuildConfiguration(default(string));
        }

        public static AppDelegate BuildConfiguration(string startupName)
        {
            var startup = new StartupLoader().Load(startupName);
            return BuildConfiguration(startup);
        }

        public static AppDelegate BuildConfiguration(Action<IAppBuilder> startup)
        {
            if (startup == null)
                throw new ArgumentNullException("startup");

            var builder = new AppBuilder();
            startup(builder);
            return builder.Materialize();
        }

        readonly IList<Delegate> _stack;

        public AppBuilder()
        {
            _stack = new List<Delegate>();
            AddAdapter<AppDelegate, AppAction>(Delegates.ToAction);
            AddAdapter<AppAction, AppDelegate>(Delegates.ToDelegate);
            AddAdapter<AppDelegate, OwinApp>(Delegates.ToApp);
            AddAdapter<OwinApp, AppDelegate>(Delegates.ToDelegate);
        }

        public IAppBuilder Use<TApp>(Func<TApp, TApp> middleware)
        {
            _stack.Add(middleware);
            return this;
        }

        public TApp Build<TApp>(Action<IAppBuilder> fork)
        {
            var b = new AppBuilder();
            fork(b);
            return b.Materialize<TApp>();
        }

        public TApp Materialize<TApp>()
        {
            var app = (Delegate)NotFound.App();
            app = _stack
                .Reverse()
                .Aggregate(app, Wrap);
            return (TApp)(Object)Adapt(app, typeof(TApp));
        }

        Delegate Wrap(Delegate app, Delegate middleware)
        {
            var middlewareFlavor = middleware.Method.ReturnType;
            var neededApp = Adapt(app, middlewareFlavor);
            return (Delegate)middleware.DynamicInvoke(neededApp);
        }

        Delegate Adapt(Delegate currentApp, Type neededFlavor)
        {
            var currentFlavor = currentApp.GetType();
            if (currentFlavor == neededFlavor)
                return currentApp;

            Func<Delegate, Delegate> adapter;
            if (_adapters.TryGetValue(Tuple.Create(currentFlavor, neededFlavor), out adapter))
                return adapter(currentApp);

            throw new Exception(string.Format("Unable to convert from {0} to {1}", currentFlavor, neededFlavor));
        }

        readonly IDictionary<Tuple<Type, Type>, Func<Delegate, Delegate>> _adapters = new Dictionary<Tuple<Type, Type>, Func<Delegate, Delegate>>();
        void AddAdapter<TCurrent, TNeeded>(Func<TCurrent, TNeeded> adapter)
        {
            _adapters.Add(Tuple.Create(typeof(TCurrent), typeof(TNeeded)), Adapter(adapter));
        }
        Func<Delegate, Delegate> Adapter<TCurrent, TNeeded>(Func<TCurrent, TNeeded> adapter)
        {
            return app => (Delegate)(Object)adapter((TCurrent)(Object)app);
        }

        public AppDelegate Build(Action<IAppBuilder> fork)
        {
            var b = new AppBuilder();
            fork(b);
            return b.Materialize<AppDelegate>();
        }

        public AppDelegate Materialize()
        {
            return Materialize<AppDelegate>();
        }
    }
}