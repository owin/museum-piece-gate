using System.Web;

namespace Gate.Hosts.AspNet
{
    public class Module : IHttpModule
    {
        public void Init(HttpApplication app)
        {
            var handler = new Handler();
            app.PostResolveRequestCache += (sender, e) => app.Context.RemapHandler(handler);
        }

        public void Dispose()
        {
        }
    }
}
