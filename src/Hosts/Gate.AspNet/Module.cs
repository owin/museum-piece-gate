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
    using AppDelegate = Action< // app
        IDictionary<string, object>, // env
        Action< // result
            string, // status
            IDictionary<string, string>, // headers
            Func< // body
                Func< // next
                    ArraySegment<byte>, // data
                    Action, // continuation
                    bool>, // async                    
                Action<Exception>, // error
                Action, // complete
                Action>>, // cancel
        Action<Exception>>; // fault

    public class Module : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication init)
        {
            var configurationString = ConfigurationManager.AppSettings["Gate.Startup"];

            var builder = new AppBuilder();
            builder.Configure(configurationString);
            Handler.Run(builder.Build());

            var appHandler = new AppHandler(builder.Build());

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