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
                .Run(Gate.Nancy.Application.Create);
        }
    }
}
