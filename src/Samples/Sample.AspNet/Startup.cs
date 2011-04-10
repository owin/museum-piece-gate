using Gate.Helpers;
using Gate.Startup;

namespace Sample.AspNet
{
    public class Startup
    {
        public void Configuration(AppBuilder builder)
        {
            builder
                .SetUrlMapper(UrlMapper.New)
                .Use(ShowExceptions.New)
                .Map("/wilson", Wilson.App)
                .Run(Gate.Nancy.Application.Create);
        }
    }
}
