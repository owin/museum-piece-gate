using System;
using System.Configuration;
using System.Web;

namespace Gate.Hosts.AspNet
{
    public class OwinModule : IHttpModule
    {
        public void Init(HttpApplication app)
        {
            // Testing this will also have the side-effect of executing the Startup class
            // which will also give end-user code an opportinuty to add routes. That is needed
            // for the case when HandleAllRequests is Disabled.
            if (AppSingleton.Instance == null)
            {
                return;
            }

            var appSetting = ConfigurationManager.AppSettings["Gate.AspNet.HandleAllRequests"];
            if (string.IsNullOrWhiteSpace(appSetting) ||
                string.Equals("Enabled", appSetting, StringComparison.InvariantCultureIgnoreCase))
            {
                var handler = new OwinHandler(null);
                app.PostResolveRequestCache += (sender, e) => app.Context.RemapHandler(handler);
            }
        }

        public void Dispose()
        {
        }
    }
}
