using System.Web.Hosting;
using Nancy;

namespace Sample.AspNet
{
    public class AspNetRootSourceProvider : IRootPathProvider
    {
        public string GetRootPath()
        {
            return HostingEnvironment.MapPath("~/");
        }
    }
}