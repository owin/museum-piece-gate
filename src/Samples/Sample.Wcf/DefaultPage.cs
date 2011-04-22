using Gate;

namespace Sample.Wcf
{
    public class DefaultPage : IApplication
    {
        public static AppDelegate Create()
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

        AppDelegate IApplication.Create()
        {
            return Create();
        }
    }
}