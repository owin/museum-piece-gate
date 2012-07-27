using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Routing;
using Gate;
using Gate.Hosts.AspNet;
using Gate.Middleware;
using Owin;
using System.Threading.Tasks;

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
            builder.UseDirect((req, res) =>
            {
                res.ContentType = "text/plain";
                res.Write("Hello from " + req.PathBase + req.Path);
                return res.EndAsync();
            });

            // a route may also be added for a given builder method.
            // this can also be done from global.asax
            RouteTable.Routes.AddOwinRoute("wilson-async", x => x.UseShowExceptions().UseContentType("text/plain").Run(Wilson.AsyncApp()));

            // a route may also be added for a given builder method.
            // this can also be done from global.asax
            RouteTable.Routes.AddOwinRoute("wilson", x => x.UseShowExceptions().UseContentType("text/plain").Run(Wilson.App()));

            // a route may also be added for a given app delegate
            // this can also be done from global.asax
            RouteTable.Routes.AddOwinRoute("raw", Raw);
        }

        void ConfigWilson(IAppBuilder builder)
        {
            builder.UseShowExceptions().Run(Wilson.App());
        }

        Task<ResultParameters> Raw(CallParameters call)
        {
            ResultParameters result = new ResultParameters();
            result.Status = 200;
            result.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { { "Content-Type", new[] { "text/plain" } } };
            result.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            result.Body = stream =>
                {
                    byte[] body = Encoding.UTF8.GetBytes("Hello from lowest-level code");
                    stream.Write(body, 0, body.Length);
                    return TaskHelpers.Completed();
                };
            return TaskHelpers.FromResult(result);
        }
    }
}
