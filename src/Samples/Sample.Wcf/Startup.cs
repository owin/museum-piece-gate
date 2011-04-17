using System;
using System.Collections.Generic;
using Gate.Helpers;
using Gate.Startup;

namespace Sample.Wcf
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
            return (env, result, fault) => new Response(result) {ContentType = "text/html"}
                .Write("<h1>Sample.Wcf</h1>")
                .Write("<p><a href='/wilson/'>Wilson</a></p>")
                .Write("<p><a href='/wilsonasync/'>Wilson (async)</a></p>")
                .Write("<p><a href='/nancy/'>Nancy</a></p>")
                .Write("<p><a href='/nancy/fileupload'>File Upload</a></p>")
                .Finish();
        }
    }
}