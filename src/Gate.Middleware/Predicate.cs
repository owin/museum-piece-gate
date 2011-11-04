using System;
using System.Collections.Generic;
using Gate;
using Gate.Owin;

namespace Gate.Middleware
{
    public static class PredicateExtensions
    {
        static IAppBuilder Predicate(this IAppBuilder builder,
            Action<Environment, Action<bool>> predicate, bool invert, AppDelegate stack)
        {
            return builder.Use(a => new PredicateMiddleware(predicate,
                invert ? stack : a, invert ? a : stack).Invoke);
        }

        public static IAppBuilder Where(this IAppBuilder builder,
            Action<Environment, Action<bool>> predicate, Action<IAppBuilder> stack)
        {
            return builder.Predicate(predicate, true, builder.Build(stack));
        }

        public static IAppBuilder Unless(this IAppBuilder builder,
            Action<Environment, Action<bool>> predicate, Action<IAppBuilder> stack)
        {
            return builder.Predicate(predicate, false, builder.Build(stack));
        }

        class PredicateMiddleware
        {
            AppDelegate pass, fail;
            Action<Environment, Action<bool>> predicate;

            public PredicateMiddleware(Action<Environment, Action<bool>> predicate, AppDelegate pass, AppDelegate fail)
            {
                this.predicate = predicate;
                this.pass = pass;
                this.fail = fail;
            }

            public void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
            {
                predicate(env as Environment ?? new Environment(env), b =>
                {
                    if (b)
                        pass(env, result, fault);
                    else
                        fail(env, result, fault);
                });
            }
        }
    }
}

