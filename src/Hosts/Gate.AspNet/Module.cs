using System.Web;

namespace Gate.AspNet
{
    public class Module : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication init)
        {
            var appHandler = AppHandlerSingleton.Instance;
            
            init.AddOnBeginRequestAsync(
                (sender, args, callback, state) =>
                {
                    var httpContext = ((HttpApplication) sender).Context;
                    return appHandler.BeginProcessRequest(new HttpContextWrapper(httpContext), callback, state);
                },
                appHandler.EndProcessRequest);
        }
    }
}
