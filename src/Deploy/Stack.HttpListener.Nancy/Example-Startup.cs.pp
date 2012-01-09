using Gate.Adapters.Nancy;
using Gate.Middleware;
using Gate.Owin;

namespace $rootnamespace$
{
    public static class Startup
    {
        public static void Configuration(IAppBuilder builder)
        {
            builder
                .RunNancy();
        }

        public static void Debug(IAppBuilder builder)
        {
            builder
                .UseShowExceptions()
                .RunNancy();
        }
    }
}
