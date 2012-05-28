using System.Web;
using System.Web.Routing;

namespace Gate.Hosts.AspNet
{
    public class OwinRouteHandler : IRouteHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new OwinHandler();
        }
    }
}