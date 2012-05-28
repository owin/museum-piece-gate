using System.Web.Routing;
using Gate;
using Gate.Hosts.AspNet;
using Owin;

namespace Samples.ViaRouting
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            RouteTable.Routes.AddOwinRoute("hello");

            builder.RunDirect((req, res) =>
            {
                res.ContentType = "text/plain";
                res.End("Hello from " + req.PathBase + req.Path);
            });
        }
    }
}
