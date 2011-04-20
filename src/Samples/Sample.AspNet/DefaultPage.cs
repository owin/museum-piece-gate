using System;
using System.Collections.Generic;
using Gate;
using Gate.Helpers;

namespace Sample.AspNet
{
    public class DefaultPage : IApplication
    {
        public static Action<IDictionary<string, object>, Action<string, IDictionary<string, string>, Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>>, Action<Exception>> Create()
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

        Action<IDictionary<string, object>, Action<string, IDictionary<string, string>, Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>>, Action<Exception>> IApplication.Create()
        {
            return Create();
        }
    }
}