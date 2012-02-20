using Gate.Adapters.Nancy;
using Gate.Middleware;
using Owin;

namespace $rootnamespace$
{
    public partial class Startup
    {
        public void Pipeline_030_SignalR(IAppBuilder builder)
        {
            builder.RunSignalR();
        }
    }
}
