using System;
using System.Threading.Tasks;
using System.Web;
using Owin;

namespace Gate.Hosts.AspNet
{
    public class OwinHandler : IHttpAsyncHandler
    {
        readonly AppDelegate _app;

        public OwinHandler(AppDelegate app)
        {
            _app = app;
        }

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            var app = _app ?? AppSingleton.Instance;
            var appHandler = new AppHandler(app);
            var task = Task.Factory.FromAsync(appHandler.BeginProcessRequest, appHandler.EndProcessRequest, new HttpContextWrapper(context), extraData);
            if (cb != null)
                task.ContinueWith(t => cb(t), TaskContinuationOptions.PreferFairness);

            return task;
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            ((Task)result).Wait();
        }

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}
