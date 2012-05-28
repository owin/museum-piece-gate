using System;
using System.Web;
using System.Web.Routing;
using Owin;

namespace Gate.Hosts.AspNet
{
    public class OwinRoute : RouteBase
    {
        readonly string _path;
        readonly AppDelegate _app;

        public OwinRoute(string path, AppDelegate app)
        {
            _path = path;
            _app = app;
        }

        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            var requestPath = httpContext.Request.AppRelativeCurrentExecutionFilePath.Substring(2) + httpContext.Request.PathInfo;
            return requestPath.StartsWith(_path, StringComparison.OrdinalIgnoreCase) ? new RouteData(this, new OwinRouteHandler(_app)) : null;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            return null;
        }
    }
}