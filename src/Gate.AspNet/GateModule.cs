using System;
using System.Threading.Tasks;
using System.Web;

namespace Gate.AspNet
{
    public class GateModule : IHttpModule
    {
        public void Init(HttpApplication httpApplication)
        {
            httpApplication.AddOnBeginRequestAsync((sender, args, callback, state) =>
            {
                var taskCompletionSource = new TaskCompletionSource<object>(state, TaskCreationOptions.PreferFairness);
                if (callback != null)
                    taskCompletionSource.Task.ContinueWith(task => callback(task), TaskContinuationOptions.ExecuteSynchronously);

                taskCompletionSource.SetResult(null);
                return taskCompletionSource.Task;
            }, ar => ((Task) ar).Wait());
        }

        public void Dispose()
        {
        }
    }
}