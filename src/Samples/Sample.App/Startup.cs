using Gate;
using Gate.Helpers;
using Nancy.Hosting.Owin;

namespace Sample.App
{
    public class Startup
    {
        public void Configuration(AppBuilder builder)
        {
            builder
                .Use(RewindableBody.Create)
                .Use(ShowExceptions.Create)
                .Use(ContentType.Create, "text/html")
                .Map("/wilson", Wilson.Create)
                .Map("/wilsonasync", Wilson.Create, true)
                .Run(Cascade.Create(
                    DefaultPage.Create(),
                    Delegates.ToDelegate(new NancyOwinHost().ProcessRequest))
                );
        }

        public void ConfigurationVariation(AppBuilder builder)
        {
            builder
                .Use<RewindableBody>()
                .Use<ShowExceptions>()
                .Use<ContentType, string>("text/html")
                .Map("/wilson", map => map.Run<Wilson>())
                .Map("/wilsonasync", map => map.Run<Wilson, bool>(true))
                .Cascade(
                    cascade => cascade.Run<DefaultPage>(),
                    cascade => cascade.Run(Delegates.ToDelegate(new NancyOwinHost().ProcessRequest))
                );
        }
    }
}