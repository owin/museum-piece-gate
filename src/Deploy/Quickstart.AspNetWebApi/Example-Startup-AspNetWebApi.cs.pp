using System.Net;
using System.Net.Http;
using System.Web.Http;
using Gate.Adapters.AspNetWebApi;
using Owin;

namespace $rootnamespace$
{
    public partial class Startup
    {
        public void Cascade_060_AspNetWebApi(IAppBuilder builder)
        {
            var config = new HttpConfiguration(new HttpRouteCollection("/"));

            config.Routes.MapHttpRoute(
                "Default",
                "{controller}",
                new {controller = "Main"});

            builder
                .RunHttpServer(config);
        }
    }

    public class MainController : ApiController
    {
        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Hello, AspNetWebApi!")
            };
        }
    }
}
