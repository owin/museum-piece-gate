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
        public static IAppBuilder Transform(this IAppBuilder builder, Action<Environment, Action<Environment>> transform)
        {
            return builder.Use(a => new TransformRequestMiddleware(a, transform).Invoke);
        }

        class TransformRequestMiddleware
        {
            AppDelegate wrapped;
            Action<Environment, Action<Environment>> transform;

            public TransformRequestMiddleware(AppDelegate wrapped,
                Action<Environment, Action<Environment>> transform)
            {
                this.wrapped = wrapped;
                this.transform = transform;
            }

            public void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
            {
                transform(env as Environment ?? new Environment(env), env0 => wrapped(env0, result, fault));
            }
        }

        public static IAppBuilder Transform(this IAppBuilder builder,
            Action<Response, Action<Response>> transform)
        {
            return builder.Use(a => new TransformResponseMiddleware(a, transform).Invoke);
        }

        class TransformResponseMiddleware
        {
            Action<Response, Action<Response>> transform;
            AppDelegate wrapped;

            public TransformResponseMiddleware(AppDelegate wrapped,
                Action<Response, Action<Response>> transform)
            {
                this.wrapped = wrapped;
                this.transform = transform;
            }

            public void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
            {
                wrapped(env, (status, headers, body) =>
                    transform(new Response(status, headers, body), r => result(r.Item1, r.Item2, r.Item3)), fault);
            }
        }
    }
}
