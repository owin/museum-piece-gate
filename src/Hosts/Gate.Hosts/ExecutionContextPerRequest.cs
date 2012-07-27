using System;
using System.Threading;
using Owin;
using System.Threading.Tasks;

namespace Gate.Hosts
{
    public static class ExecutionContextPerRequest
    {
        public static AppDelegate Middleware(AppDelegate app)
        {
            return
                call =>
                {
                    TaskCompletionSource<ResultParameters> tcs = new TaskCompletionSource<ResultParameters>();
                    ExecutionContext.SuppressFlow();
                    ThreadPool.QueueUserWorkItem(
                        _ =>
                        {
                            app(call).Then(result => WrapBodyDelegate(result)).CopyResultToCompletionSource(tcs);
                        },
                        null);
                    ExecutionContext.RestoreFlow();
                    return tcs.Task;
                };
        }

        static ResultParameters WrapBodyDelegate(ResultParameters result)
        {
            if (result.Body != null)
            {
                var nestedBody = result.Body;
                result.Body = stream =>
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    ExecutionContext.SuppressFlow();
                    ThreadPool.QueueUserWorkItem(
                        _ => nestedBody(stream)
                            .Then(() => { bool ignored = tcs.TrySetResult(null); })
                            .Catch(errorInfo => 
                            { 
                                bool ignored = tcs.TrySetException(errorInfo.Exception); 
                                return errorInfo.Handled(); 
                            }),
                        null);
                    ExecutionContext.RestoreFlow();
                    return tcs.Task;
                };
            }

            return result;
        }
    }
}
