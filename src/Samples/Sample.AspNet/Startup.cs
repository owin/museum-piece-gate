using Gate.Helpers;
using Gate.Startup;

namespace Sample.AspNet
{
    public class Startup
    {
        public void Configuration(AppBuilder builder)
        {
            builder
                .Use(ShowExceptions.Create)
                .Map("/wilson", Wilson.Create)
                .Map("/wilsonasync", Wilson.AppAsync)
                .Run(new Nancy.Hosting.Owin.NancyOwinHost().ProcessRequest);
        }
    }
}