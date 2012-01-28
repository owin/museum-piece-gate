using Gate;
using Gate.Middleware;
using Gate.Owin;
using Gate.Adapters.Nancy;

namespace Sample.App
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder
                //.UseRewindableBody()
                .UseShowExceptions()
                .UseContentType()
                .Map("/wilson", map => map.Run(Wilson.App))
                .Map("/wilsonasync", map => map.Run(Wilson.App, true))
                .RunCascade(
                    x => x.RunDefaultPage(),
                    x => x.RunNancy());
        }
    }
}
