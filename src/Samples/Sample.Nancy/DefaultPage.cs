using System;
using Gate;
using Owin;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Sample.Nancy
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class DefaultPage
    {
        public static AppFunc App()
        {
            return new DefaultPage().Invoke;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var request = new Request(env);
            var response = new Response(env);

            if (request.Path == "/")
            {
                response.Status = "200 OK";
                response.ContentType = "text/html";
                response.Write("<h1>Sample.App</h1>");
                response.Write("<p><a href='{0}/wilson/'>Wilson</a></p>", request.PathBase);
                response.Write("<p><a href='{0}/wilsonasync/'>Wilson (async)</a></p>", request.PathBase);
                response.Write("<p><a href='{0}/nancy/'>Nancy</a></p>", request.PathBase);
                response.Write("<p><a href='{0}/fileupload'>File Upload</a></p>", request.PathBase);
                return response.EndAsync();
            }
            else
            {
                response.StatusCode = 404;
                return response.EndAsync();
            }
        }
    }
}