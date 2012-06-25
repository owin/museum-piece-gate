using System;
using Owin;

namespace Gate
{
    public static class AppBuilderInlineExtensions
    {
        public static IAppBuilder MapDirect(this IAppBuilder builder, string path, Action<Request, Response> app)
        {
            return builder.Map(path, map => map.RunDirect(app));
        }

        public static IAppBuilder RunDirect(this IAppBuilder builder, Action<Request, Response> app)
        {
            return builder.Run<AppDelegate>(() => (call, callback) => app(new Request(call), new Response(callback)));
        }

        public static IAppBuilder UseDirect(this IAppBuilder builder, Action<Request, Response, Action> app)
        {
            return builder.Use<AppDelegate>(next => (call, callback) => app(new Request(call), new Response(callback), () => next(call, callback)));
        }
    }
}
