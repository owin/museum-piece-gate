using System;
using Owin;

namespace Gate
{
    public static class AppBuilderInlineExtensions
    {
        public static IAppBuilder MapInline(this IAppBuilder builder, string path, Action<Request, Response> app)
        {
            return builder.Map(path, map => map.RunInline(app));
        }

        public static IAppBuilder RunInline(this IAppBuilder builder, Action<Request, Response> app)
        {
            return builder.Run<AppDelegate>(() => (env, result, fault) => app(new Request(env), new Response(result)));
        }

        public static IAppBuilder UseInline(this IAppBuilder builder, Action<Request, Response, Action> app)
        {
            return builder.Use<AppDelegate>(next => (env, result, fault) => app(new Request(env), new Response(result), () => next(env, result, fault)));
        }
    }
}
