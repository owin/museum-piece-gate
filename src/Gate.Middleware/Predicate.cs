using System;
using Gate.Owin;

namespace Gate.Middleware
{
    public static class Predicate
    {
        public static IAppBuilder Where(this IAppBuilder builder,
            Action<Environment, Action<bool>> predicate, Action<IAppBuilder> stack)
        {
            return builder.Use(Middleware, predicate, builder.Build<AppDelegate>(stack));
        }

        public static IAppBuilder Unless(this IAppBuilder builder,
            Action<Environment, Action<bool>> predicate, Action<IAppBuilder> stack)
        {
            return builder.Use(Middleware, Not(predicate), builder.Build<AppDelegate>(stack));
        }

        static Action<Environment, Action<bool>> Not(Action<Environment, Action<bool>> predicate)
        {
            return (env, cb) => predicate(env, passed => cb(!passed));
        }

        public static AppDelegate Middleware(AppDelegate app, Action<Environment, Action<bool>> predicate, AppDelegate pass)
        {
            return (env, result, fault) => predicate(new Environment(env), passed => (passed ? pass : app)(env, result, fault));
        }
    }
}

