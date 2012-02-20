using Gate;
using Gate.Middleware;
using Owin;

namespace $rootnamespace$
{
    public partial class Startup
    {
        public void Pipeline_040_Wilson(IAppBuilder builder)
        {
            builder
                .Map("/wilson", map => map
                    .UseShowExceptions()
                    .Run(Wilson.App))
                .Map("/wilsonasync", map => map
                    .UseShowExceptions()
                    .Run(Wilson.App, true));
        }
    }
}
