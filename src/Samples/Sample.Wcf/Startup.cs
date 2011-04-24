using Gate.Helpers;
using Gate.Startup;
using Nancy.Hosting.Owin;

namespace Sample.Wcf
{
    public class Startup
    {
        public void Configuration(AppBuilder builder)
        {
            var nancyOwinHost = new NancyOwinHost();
            builder
                .Use(ShowExceptions.Create)
                .Map("/wilson", Wilson.Create)
                .Map("/wilsonasync", Wilson.Create, true)
                .Map("/nancy", map => map
                    .Use(ContentType.Create, "text/html")
                    .Ext.Run(nancyOwinHost.ProcessRequest))
                .Run(DefaultPage.Create);
        }

        public void ConfigurationVariation(AppBuilder builder)
        {
            builder
                .Use<ShowExceptions>()
                .Map("/wilson", map => map.Run<Wilson>())
                .Map("/wilsonasync", map => map.Run<Wilson, bool>(true))
                .Map("/nancy", map => map
                    .Use<ContentType, string>("text/html")
                    .Ext.Run(new NancyOwinHost().ProcessRequest))
                .Run<DefaultPage>();
        }
    }
}