using System;
using System.Collections.Generic;
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
                .Map("/nancy", new Nancy.Hosting.Owin.NancyOwinHost().ProcessRequest)
                .Run(DefaultPage);
        }

        static Action<IDictionary<string, object>, Action<string, IDictionary<string, string>, Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>>, Action<Exception>> DefaultPage()
        {
            return (env, result, fault) =>
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
