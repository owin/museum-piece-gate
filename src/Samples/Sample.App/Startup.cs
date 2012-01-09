using Gate;
using Gate.Helpers;
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
                .UseRewindableBody()
                .UseShowExceptions()
                .UseContentType()
                .Map("/wilson", map => map.Run(Wilson.App))
                .Map("/wilsonasync", map => map.Run(Wilson.App, true))
                .Cascade(
                    x => x.Run(DefaultPage.App),
                    x => x.RunNancy());
        }
    }
}
