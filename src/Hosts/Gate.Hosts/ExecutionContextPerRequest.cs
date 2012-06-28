using System;
using System.Threading;
using Owin;

namespace Gate.Hosts
{
    public static class ExecutionContextPerRequest
    {
        public static AppDelegate Middleware(AppDelegate app)
        {
            return
                (call, callback) =>
                {
                    ExecutionContext.SuppressFlow();
                    ThreadPool.QueueUserWorkItem(
                        _ =>
                        {
                            var context = ExecutionContext.Capture();
                            app(call, WrapCallbackDelegate(context, callback));
                        },
                        null);
                    ExecutionContext.RestoreFlow();
                };
        }

        static Action<ResultParameters, Exception> WrapCallbackDelegate(ExecutionContext context, Action<ResultParameters, Exception> callback)
        {
            return (result, error) =>
            {
                result.Body = WrapBodyDelegate(context, result.Body);
                callback(result, error);
            };
        }

        static BodyDelegate WrapBodyDelegate(ExecutionContext context, BodyDelegate body)
        {
            if (body == null)
            {
                return null;
            }

            return (write, end, cancellationToken) => ExecutionContext.Run(
                context.CreateCopy(),
                _ => body(WrapWriteDelegate(context, write), end, cancellationToken),
                null);
        }

        static Func<ArraySegment<byte>, Action<Exception>, TempEnum> WrapWriteDelegate(ExecutionContext context, Func<ArraySegment<byte>, Action<Exception>, TempEnum> write)
        {
            return (data, callback) =>
            {
                if (callback == null)
                {
                    return write(data, null);
                }

                return write(data, ex => ExecutionContext.Run(context.CreateCopy(), state => callback((Exception)state), ex));
            };
        }
    }
}
