using System;
using Gate;
using Owin;
using System.Threading.Tasks;

namespace Sample.Nancy
{
    public class DefaultPage
    {
        public static AppDelegate App()
        {
            return new DefaultPage().Invoke;
        }

        public Task<ResultParameters> Invoke(CallParameters call)
        {
            var request = new Request(call);
            var response = new Response();

            if (request.Path == "/")
            {
                response.Status = "200 OK";
                response.ContentType = "text/html";
                response.StartAsync().Then(resp1 =>
                {
                    resp1.Write("<h1>Sample.App</h1>");
                    resp1.Write("<p><a href='{0}/wilson/'>Wilson</a></p>", request.PathBase);
                    resp1.Write("<p><a href='{0}/wilsonasync/'>Wilson (async)</a></p>", request.PathBase);
                    resp1.Write("<p><a href='{0}/nancy/'>Nancy</a></p>", request.PathBase);
                    resp1.Write("<p><a href='{0}/fileupload'>File Upload</a></p>", request.PathBase);
                    resp1.End();
                });
                return response.ResultTask;
            }
            else
            {
                response.StatusCode = 404;
                return response.EndAsync();
            }
        }
    }
}