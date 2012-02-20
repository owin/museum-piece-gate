using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Sample.AspNetWebApi
{
    public class MainController : ApiController
    {
        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("DateTime.Now " + System.DateTime.Now)
            };
        }
    }
}