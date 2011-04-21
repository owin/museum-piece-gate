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
                .Use(ShowExceptions.Create)
                .Map("/wilson", Wilson.Create)
                .Map("/wilsonasync", Wilson.AppAsync)
                // this could be cleaned up if NancyOwinHost exposed a member which *returned* AppDelegate. 
                //
                // unfortunately, due a bug in the C# compiler, you can't just say
                //
                // .Map("/nancy", new NancyOwinHost().ProcessRequest)
                //
                // where ProcessRequest conforms to AppDelegate.
                // 
                // see: 
                // http://stackoverflow.com/questions/4466859/delegate-system-action-does-not-take-0-arguments-is-this-a-c-compiler-bug
                .Map("/nancy", (IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault) =>
                    {
                        new Nancy.Hosting.Owin.NancyOwinHost().ProcessRequest(env, (status, headers, body) => result(status, headers, body), fault);
                    })
                .Run(DefaultPage);
        }

        static AppDelegate DefaultPage()
        {
            return (IDictionary<string, object> env, ResultDelegate result, Action<Exception> fault) =>
            {
                var request = new Request(env);
                new Response(result) {ContentType = "text/html"}
                    .Write("<h1>Sample.AspNet</h1>")
                    .Write("<p><a href='{0}/wilson/'>Wilson</a></p>", request.PathBase)
                    .Write("<p><a href='{0}/wilsonasync/'>Wilson (async)</a></p>", request.PathBase)
                    .Write("<p><a href='{0}/nancy/'>Nancy</a></p>", request.PathBase)
                    .Write("<p><a href='{0}/nancy/fileupload'>File Upload</a></p>", request.PathBase)
                    .Finish();
            };
        }
    }
}
