using Gate.Helpers;
using Gate.Startup;

namespace Sample.Wcf
{
    public class Startup
    {
        public void Configuration(AppBuilder builder)
        {
            builder
                .SetUrlMapper(UrlMapper.Create)
                .Use(ShowExceptions.Create)
                .Map("/wilson", Wilson.Create);
        }
    }
}
