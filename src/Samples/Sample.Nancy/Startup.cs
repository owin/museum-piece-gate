using System.Reflection;
using Gate;
using Gate.Adapters.Nancy;
using Gate.Middleware;
using Owin;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Sample.Nancy
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class Startup
    {
        public AppFunc Configuration(IDictionary<string,object> props)
        {
            return NancyAdapter.App();
        }

        public void Configuration2(IAppBuilder builder)
        {
            Assembly.Load("Nancy.ViewEngines.Spark");

            builder
                .UseShowExceptions()
                .UseContentType()
                .Map("/wilson", map => map.Run(new Wilson()))
                .Map("/wilsonasync", map => map.Run(new WilsonAsync()))
                .RunNancy();
            /*
                .RunCascade(
                    x => x.Run(new DefaultPage()),
                    x => x.RunNancy());
             */
        }


        public void Alternative(IAppBuilder builder)
        {
            Assembly.Load("Nancy.ViewEngines.Spark");

            builder
                .UseShowExceptions()
                .UseContentType()
                .Map("/wilson", Wilson.App())
                .Map("/wilsonasync", Wilson.App(true))
                // .UseCascade(DefaultPage.App())
                .RunNancy();
        }

        public void AnotherAlternative(IAppBuilder builder)
        {
            Assembly.Load("Nancy.ViewEngines.Spark");

            builder
                .UseFunc<AppFunc>(ShowExceptions.Middleware)
                .UseType<ContentType>()
                .Map("/wilson", Wilson.App())
                .Map("/wilsonasync", Wilson.App(true))
                .Use(NancyAdapter.App());
            /*
                .RunCascade(
                    DefaultPage.App(), 
                    NancyAdapter.App());
             */
        }
    }
}
