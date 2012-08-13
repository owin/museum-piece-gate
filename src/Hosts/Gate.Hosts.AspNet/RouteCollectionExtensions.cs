using System;
using System.Web.Routing;
using Owin;
using Owin.Builder;

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
            var builder = new AppBuilder();
            configuration(builder);
            var app = (AppDelegate)builder.Build(typeof(AppDelegate));
            routes.Add(new OwinRoute(path, app));
        }

        public static void AddOwinRoute(this RouteCollection routes, string path, IAppBuilder builder, Action<IAppBuilder> configuration)
        {
            var nested = builder.New();
            configuration(nested);
            var app = (AppDelegate)nested.Build(typeof(AppDelegate));
            routes.Add(new OwinRoute(path, app));
        }
    }
}
