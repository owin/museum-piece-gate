using System;
using System.Web.Routing;
using Gate.Builder;
using Owin;

namespace Gate.Hosts.AspNet
{
    public static class RouteCollectionExtensions
    {
        public static void AddOwinRoute(this RouteCollection routes, string path)
        {
            routes.Add(new OwinRoute(path, null));
        }

        public static void AddOwinRoute(this RouteCollection routes, string path, AppDelegate app)
        {
            routes.Add(new OwinRoute(path, app));
        }

        public static void AddOwinRoute(this RouteCollection routes, string path, Action<IAppBuilder> configuration)
        {
            var app = AppBuilder.BuildPipeline(configuration);
            routes.Add(new OwinRoute(path, app));
        }

        public static void AddOwinRoute(this RouteCollection routes, string path, IAppBuilder builder, Action<IAppBuilder> configuration)
        {
            var app = builder.Build<AppDelegate>(configuration);
            routes.Add(new OwinRoute(path, app));
        }
    }
}
