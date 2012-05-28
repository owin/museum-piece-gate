using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Routing;
using Gate;
using Gate.Hosts.AspNet;
using Gate.Middleware;
using Owin;

namespace Samples.ViaRouting
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            // routes can be added for each path prefix that should be
            // mapped to owin
            RouteTable.Routes.AddOwinRoute("hello");
            RouteTable.Routes.AddOwinRoute("world");

            // the routes above will be map onto whatever is added to
            // the IAppBuilder builder that was passed into this method
            builder.RunDirect((req, res) =>
            {
                res.ContentType = "text/plain";
                res.End("Hello from " + req.PathBase + req.Path);
            });


            // a route may also be added for a given builder method.
            // this can also be done from global.asax
            RouteTable.Routes.AddOwinRoute("wilson", ConfigWilson);

            // a route may also be added for a given app delegate
            // this can also be done from global.asax
            RouteTable.Routes.AddOwinRoute("raw", Raw);
        }

        void ConfigWilson(IAppBuilder builder)
        {
            builder.UseShowExceptions().Run(Wilson.App());
        }

        void Raw(IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault)
        {
            result(
                "200 OK",
                new Dictionary<string, IEnumerable<string>> { { "Content-Type", new[] { "text/plain" } } },
                (write, flush, end, cancel) =>
                {
                    write(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello from lowest-level code")));
                    end(null);
                });
        }
    }
}
