using Gate;
using Gate.Owin;

namespace Sample.App
{
    public static class DefaultPage
    {
        public static IAppBuilder RunDefaultPage(this IAppBuilder builder)
        {
            return builder.Run(App);
        }

        public static AppDelegate App()
        {
            return (env, result, fault) =>
            {
                var request = new Request(env);
                var response = new Response(result);

                if (request.Path == "/")
                {
                    response.Status = "200 OK";
                    response.ContentType = "text/html";
                    response.Start(() => response
                        .Write("<h1>Sample.App</h1>")
                        .Write("<p><a href='{0}/wilson/'>Wilson</a></p>", request.PathBase)
                        .Write("<p><a href='{0}/wilsonasync/'>Wilson (async)</a></p>", request.PathBase)
                        .Write("<p><a href='{0}/nancy/'>Nancy</a></p>", request.PathBase)
                        .Write("<p><a href='{0}/fileupload'>File Upload</a></p>", request.PathBase)
                        .End());
                }
                else
                {
                    NotFound.Call(env, result, fault);
                }
            };
        }
    }
}