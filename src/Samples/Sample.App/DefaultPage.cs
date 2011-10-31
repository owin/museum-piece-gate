using Gate;
using Gate.Helpers;

namespace Sample.App
{
    public class DefaultPage 
    {
        public static AppDelegate App()
        {
            return (env, result, fault) =>
            {
                var request = new Request(env);
                if (request.Path == "/")
                {
                    new Response(result) {ContentType = "text/html"}
                        .Write("<h1>Sample.App</h1>")
                        .Write("<p><a href='{0}/wilson/'>Wilson</a></p>", request.PathBase)
                        .Write("<p><a href='{0}/wilsonasync/'>Wilson (async)</a></p>", request.PathBase)
                        .Write("<p><a href='{0}/nancy/'>Nancy</a></p>", request.PathBase)
                        .Write("<p><a href='{0}/fileupload'>File Upload</a></p>", request.PathBase)
                        .Finish();
                }
                else
                {
                    NotFound.Invoke(env, result, fault);
                }
            };
        }
    }
}