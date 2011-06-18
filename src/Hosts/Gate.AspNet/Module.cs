using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Gate.Startup;

namespace Gate.AspNet
{
    public class Module : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication init)
        {
            var configurationString = ConfigurationManager.AppSettings["Gate.Startup"];
            var app = AppBuilder.BuildFromConfiguration(configurationString);
            var appHandler = new AppHandler(app);

            init.AddOnBeginRequestAsync(
                (sender, args, callback, state) =>
                {
                    var httpContext = ((HttpApplication) sender).Context;
                    return appHandler.BeginProcessRequest(new HttpContextWrapper(httpContext), callback, state);
                },
                appHandler.EndProcessRequest);
        }

    }
}