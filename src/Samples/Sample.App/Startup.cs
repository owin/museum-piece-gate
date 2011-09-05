using Gate;
using Gate.Helpers;
using Nancy.Hosting.Owin;

namespace Sample.App
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            var nancyOwinHost = new NancyOwinHost();
            builder
                .Use(RewindableBody.Create)
                .Use(ShowExceptions.Create)
                .Use(ContentType.Create, "text/html")
                .Map("/wilson", Wilson.Create)
                .Map("/wilsonasync", Wilson.Create, true)
                .Run(Cascade.Create(
                    DefaultPage.Create(),
                    Delegates.ToDelegate(nancyOwinHost.ProcessRequest))
                );
        }

        public void ConfigurationVariation(IAppBuilder builder)
        {
            builder
                .Use<RewindableBody>()
                .Use<ShowExceptions>()
                .Use<ContentType, string>("text/html")
                .Map("/wilson", map => map.Run<Wilson>())
                .Map("/wilsonasync", map => map.Run<Wilson, bool>(true))
                .Cascade(
                    cascade => cascade.Run<DefaultPage>(),
                    cascade => cascade.Run(new NancyOwinHost().ProcessRequest)
                );
        }
    }
}