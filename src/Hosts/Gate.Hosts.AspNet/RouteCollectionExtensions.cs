using System.Web.Routing;

namespace Gate.Hosts.AspNet
{
    public static class RouteCollectionExtensions
    {
        public static void AddOwinRoute(this RouteCollection routes, string path)
        {
            routes.Add(new OwinRoute(path));
        }
    }
}
