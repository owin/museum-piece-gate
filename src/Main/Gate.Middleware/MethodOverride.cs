using Owin;

namespace Gate.Middleware
{
    // Reads the X-Http-Method-Override header value to replace the request method. This is useful when 
    // intermediate client, proxy, firewall, or server software does not understand or permit the necessary 
    // methods.
    public static class MethodOverride
    {
        public static IAppBuilder UseMethodOverride(this IAppBuilder builder)
        {
            return builder.UseFunc<AppDelegate>(Middleware);
        }

        public static AppDelegate Middleware(AppDelegate app)
        {
            return call =>
            {
                var req = new Request(call);
                var method = req.Headers.GetHeader("x-http-method-override");
                if (!string.IsNullOrWhiteSpace(method))
                    req.Method = method;

                return app(call);
            };
        }
    }
}
