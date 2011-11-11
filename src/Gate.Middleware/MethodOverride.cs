using Gate.Owin;

namespace Gate.Middleware
{
    public static class MethodOverride
    {
        public static IAppBuilder UseMethodOverride(this IAppBuilder builder)
        {
            return builder.Use(Middleware);
        }

        public static AppDelegate Middleware(AppDelegate app)
        {
            return (env, result, fault) =>
            {
                var req = new Request(env);
                string method;
                if (req.Headers.TryGetValue("x-http-method-override", out method))
                    req.Method = method;

                app(env, result, fault);
            };
        }
    }
}
