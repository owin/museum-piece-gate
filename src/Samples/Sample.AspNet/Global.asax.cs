using System;
using Gate.AspNet;

namespace Sample.AspNet {
    public class Global : System.Web.HttpApplication {
        protected void Application_Start(object sender, EventArgs e) {
            Host.Run(Gate.Nancy.Application.Create());
        }
    }
}
