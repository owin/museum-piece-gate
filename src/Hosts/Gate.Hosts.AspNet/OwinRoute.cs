using System;
using System.Web;
using System.Web.Routing;

namespace Gate.Hosts.AspNet
{
    public class OwinRoute : RouteBase
    {
        readonly string _path;

        public OwinRoute(string path)
        {
            _path = path;
        }

        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            var requestPath = httpContext.Request.AppRelativeCurrentExecutionFilePath.Substring(2) + httpContext.Request.PathInfo;
            return requestPath.StartsWith(_path, StringComparison.OrdinalIgnoreCase) ? new RouteData(this, new OwinRouteHandler()) : null;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            return null;
        }
    }
}