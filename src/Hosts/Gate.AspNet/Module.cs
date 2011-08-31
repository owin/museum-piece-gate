using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Gate;

namespace Gate.AspNet
{
    public class Module : IHttpModule
    {
        private static AppHandler _appHandler;

        public void Dispose()
        {
        }

        public void Init(HttpApplication init)
        {
            lock(typeof(Module))
            {
                if (_appHandler == null)
                {
                    var configurationString = ConfigurationManager.AppSettings["Gate.Startup"];
                    var app = AppBuilder.BuildConfiguration(configurationString);
                    _appHandler = new AppHandler(app);
                }
            }

            init.AddOnBeginRequestAsync(
                (sender, args, callback, state) =>
                {
                    var httpContext = ((HttpApplication) sender).Context;
                    return _appHandler.BeginProcessRequest(new HttpContextWrapper(httpContext), callback, state);
                },
                _appHandler.EndProcessRequest);
        }

    }
}
