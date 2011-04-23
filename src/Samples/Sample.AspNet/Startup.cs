using System;
using System.Collections.Generic;
using Gate;
using Gate.Helpers;
using Gate.Startup;

namespace Sample.AspNet
{
    public class Startup
    {
        public void Configuration(AppBuilder builder)
        {
            builder
                //.Use(ShowExceptions.Create)
                .Map("/wilson", Wilson.Create)
                .Map("/wilsonasync", Wilson.CreateAsync)
                .Map("/nancy", Delegates.ToDelegate(new Nancy.Hosting.Owin.NancyOwinHost().ProcessRequest))
                .Run(DefaultPage.Create);
        }
        
        public void Configuration_variation(AppBuilder builder)
        {
            var nancyOwinHost = new Nancy.Hosting.Owin.NancyOwinHost();
            builder
                .Use<ShowExceptions>()
                .Map("/wilson", map => map.Run<Wilson>())
                .Map("/wilsonasync", map => map.Run<Wilson, bool>(true))
                .Map("/nancy", map => map.Run(Delegates.ToDelegate(nancyOwinHost.ProcessRequest)))
                .Run<DefaultPage>();
        }
    }
}
