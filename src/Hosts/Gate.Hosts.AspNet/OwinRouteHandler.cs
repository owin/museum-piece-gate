using System.Web;
using System.Web.Routing;
using Owin;

namespace Gate.Hosts.AspNet
{
    public class OwinRouteHandler : IRouteHandler
    {
        readonly AppDelegate _app;

        public OwinRouteHandler(AppDelegate app)
        {
            _app = app;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new OwinHandler(_app);
        }
    }
}