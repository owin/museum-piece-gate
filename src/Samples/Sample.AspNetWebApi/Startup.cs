using System.Web.Http;
using Gate.Adapters.AspNetWebApi;
using Owin;

namespace Sample.AspNetWebApi
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            var config = new HttpConfiguration(new HttpRouteCollection(""));
            config.Routes.MapHttpRoute("Default", "{controller}", new { controller = "Main" });
            
            builder.RunHttpServer(config);
        }
    }
}
