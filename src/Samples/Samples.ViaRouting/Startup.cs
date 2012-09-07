using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Routing;
using Gate;
using Gate.Middleware;
using Owin;
using System.Threading.Tasks;
using Microsoft.AspNet.Owin;

namespace Samples.ViaRouting
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using System.IO;

    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            // routes can be added for each path prefix that should be
            // mapped to owin
            RouteTable.Routes.MapOwinRoute("hello");
            RouteTable.Routes.MapOwinRoute("world");

            // the routes above will be map onto whatever is added to
            // the IAppBuilder builder that was passed into this method
            builder.UseGate((req, res) =>
            {
                res.ContentType = "text/plain";
                res.Write("Hello from " + req.PathBase + req.Path);
            });

            // a route may also be added for a given builder method.
            // this can also be done from global.asax
            RouteTable.Routes.MapOwinRoute("wilson-async", x => x.UseShowExceptions().UseContentType("text/plain").Run(WilsonAsync.App()));

            // a route may also be added for a given builder method.
            // this can also be done from global.asax
            RouteTable.Routes.MapOwinRoute("wilson", x => x.UseShowExceptions().UseContentType("text/plain").Run(Wilson.App()));

            // a route may also be added for a given app delegate
            // this can also be done from global.asax
            RouteTable.Routes.MapOwinRoute<AppFunc>("raw", Raw);
        }

        void ConfigWilson(IAppBuilder builder)
        {
            builder.UseShowExceptions().Run(Wilson.App());
        }

        Task Raw(IDictionary<string, object> env)
        {
            env[OwinConstants.ResponseStatusCode] = 200;
            var headers = (IDictionary<string, string[]>)env[OwinConstants.ResponseHeaders];
            headers.Add("Content-Type", new string[] { "text/plain" });
            byte[] body = Encoding.UTF8.GetBytes("Hello from lowest-level code");
            var output = (Stream)env[OwinConstants.ResponseBody];
            output.Write(body, 0, body.Length);
            return TaskHelpers.Completed();
        }
    }
}
