using Gate.Adapters.Nancy;
using Nancy;
using Owin;

namespace $rootnamespace$
{
    public partial class Startup
    {
        public void Cascade_090_Nancy(IAppBuilder builder)
        {
            builder.RunNancy();
        }
    }

    public class MainModule : NancyModule
    {
        public MainModule()
        {
            Get["/nancy"] = _ => "Hello nancy!";
        }
    }
}
