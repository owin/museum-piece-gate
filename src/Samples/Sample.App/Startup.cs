﻿using Gate;
using Gate.Builder;
using Gate.Helpers;
using Gate.Middleware;
using Nancy.Hosting.Owin;

namespace Sample.App
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            var nancyOwinHost = new NancyOwinHost();
            builder
                .UseRewindableBody()
                .UseShowExceptions()
                .UseContentType()
                .Map("/wilson", map => map.Run(Wilson.App))
                .Map("/wilsonasync", map => map.Run(Wilson.App, true))
                .Cascade(DefaultPage.App(), Delegates.ToDelegate(nancyOwinHost.ProcessRequest));
        }
    }
}