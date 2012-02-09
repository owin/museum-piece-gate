using System.Reflection;
using Gate;
using Gate.Adapters.Nancy;
using Gate.Middleware;
using Owin;

namespace Sample.Nancy
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            Assembly.Load("Nancy.ViewEngines.Spark");

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
