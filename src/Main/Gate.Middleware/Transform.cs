using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Owin;

namespace Gate.Middleware
{
    using Response = Tuple<string, IDictionary<string, string>, BodyDelegate>;

    public static class TransformExtesnsions
    {
        public static IAppBuilder Transform(this IAppBuilder builder, Action<Environment, Action<Environment>, Action<Exception>> transform)
        {
            return builder.Use(a => new TransformRequestMiddleware(a, transform).Invoke);
        }

        class TransformRequestMiddleware
        {
            AppDelegate wrapped;
            Action<Environment, Action<Environment>, Action<Exception>> transform;

            public TransformRequestMiddleware(AppDelegate wrapped,
                Action<Environment, Action<Environment>, Action<Exception>> transform)
            {
                this.wrapped = wrapped;
                this.transform = transform;
            }

            public void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
            {
                try
                {
                    transform(env as Environment ?? new Environment(env), env0 => wrapped(env0, result, fault), fault);
                }
                catch (Exception e)
                {
                    fault(e);
                }
            }
        }

        public static IAppBuilder Transform(this IAppBuilder builder,
            Action<Response, Action<Response>, Action<Exception>> transform)
        {
            return builder.Use(a => new TransformResponseMiddleware(a, transform).Invoke);
        }

        class TransformResponseMiddleware
        {
            Action<Response, Action<Response>, Action<Exception>> transform;
            AppDelegate wrapped;

            public TransformResponseMiddleware(AppDelegate wrapped,
                Action<Response, Action<Response>, Action<Exception>> transform)
            {
                this.wrapped = wrapped;
                this.transform = transform;
            }

            public void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
            {
                wrapped(env, (status, headers, body) => {
                    try
                    {
                        transform(new Response(status, headers, body), r => result(r.Item1, r.Item2, r.Item3), fault);
                    }
                    catch (Exception e)
                    {
                        fault(e);
                    }
                }, fault);
            }
        }
    }
}
