using Gate.Helpers;
using Gate.Startup;

namespace Sample.Wcf
{
    public class Startup
    {
        public void Configuration(AppBuilder builder)
        {
            builder
                .Use(ShowExceptions.Create)
                .Map("/wilson", Wilson.Create);
        }
    }
}
