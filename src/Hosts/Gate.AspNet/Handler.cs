using System;
using System.Threading.Tasks;
using System.Web;

namespace Gate.AspNet
{
    public class Handler : IHttpAsyncHandler
    {
        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            var appHandler = AppHandlerSingleton.Instance;
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
