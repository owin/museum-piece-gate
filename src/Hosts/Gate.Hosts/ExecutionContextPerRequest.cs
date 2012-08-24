using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gate.Hosts
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class ExecutionContextPerRequest
    {
        public static AppFunc Middleware(AppFunc app)
        {
            return
                call =>
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    ExecutionContext.SuppressFlow();
                    ThreadPool.QueueUserWorkItem(
                        _ =>
                        {
                            app(call)
                                .Then(() => tcs.TrySetResult(null))
                                .Catch(errorInfo =>
                                {
                                    tcs.TrySetException(errorInfo.Exception);
                                    return errorInfo.Handled();
                                });
                        },
                        null);
                    ExecutionContext.RestoreFlow();
                    return tcs.Task;
                };
        }
    }
}
