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
                .UseShowExceptions()
                .UseContentType()
                .Map("/wilson", map => map.Run(Wilson.App))
                .Map("/wilsonasync", map => map.Run(Wilson.App, true))
                .RunCascade(
                    x => x.RunDefaultPage(),
                    x => x.RunNancy());
        }


        public void Alternative(IAppBuilder builder)
        {
            Assembly.Load("Nancy.ViewEngines.Spark");
            
            builder
                .UseShowExceptions()
                .UseContentType()
                .Map("/wilson", Wilson.App())
                .Map("/wilsonasync", Wilson.App(true))
                .UseCascade(DefaultPage.App())
                .RunNancy();
        }

        public void AnotherAlternative(IAppBuilder builder)
        {
            Assembly.Load("Nancy.ViewEngines.Spark");

            builder
                .Use(ShowExceptions.Middleware)
                .Use(ContentType.Middleware)
                .Map("/wilson", Wilson.App())
                .Map("/wilsonasync", Wilson.App(true))
                .Run(Cascade.App, DefaultPage.App(), NancyAdapter.App());
        }
    }
}
