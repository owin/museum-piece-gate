using Gate;
using Owin;

namespace Sample.Nancy
{
    public static class DefaultPage
    {
        public static IAppBuilder RunDefaultPage(this IAppBuilder builder)
        {
            return builder.Run(App);
        }

        public static AppDelegate App()
        {
            return call =>
            {
                var request = new Request(call);
                var response = new Response();

                if (request.Path == "/")
                {
                    response.Status = "200 OK";
                    response.ContentType = "text/html";
                    response.Body = new ResponseBody((body) =>
                    {
                        body.Write("<h1>Sample.App</h1>");
                        body.Write("<p><a href='{0}/wilson/'>Wilson</a></p>", request.PathBase);
                        body.Write("<p><a href='{0}/wilsonasync/'>Wilson (async)</a></p>", request.PathBase);
                        body.Write("<p><a href='{0}/nancy/'>Nancy</a></p>", request.PathBase);
                        body.Write("<p><a href='{0}/fileupload'>File Upload</a></p>", request.PathBase);
                        return body.EndBodyAsync();
                    });
                    return response.EndAsync();
                }
                else
                {
                    response.StatusCode = 404;
                    return response.EndAsync();
                }
            };
        }
    }
}