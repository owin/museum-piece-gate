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
                .Map("/wilson", map => map.Run(new Wilson()))
                .Map("/wilsonasync", map => map.Run(new WilsonAsync()))
                .RunCascade(
                    x => x.Run(new DefaultPage()),
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
                .UseFunc<AppDelegate>(ShowExceptions.Middleware)
                .UseType<ContentType>()
                .Map("/wilson", Wilson.App())
                .Map("/wilsonasync", Wilson.App(true))
                .RunCascade(
                    DefaultPage.App(), 
                    NancyAdapter.App());
        }
    }
}
