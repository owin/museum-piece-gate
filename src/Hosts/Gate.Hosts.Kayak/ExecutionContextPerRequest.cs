using System;
using System.Threading;
using Owin;

namespace Gate.Hosts.Kayak
{
    public static class ExecutionContextPerRequest
    {
        public static AppDelegate Middleware(AppDelegate app)
        {
            return
                (env, result, fault) =>
                {
                    ExecutionContext.SuppressFlow();
                    ThreadPool.QueueUserWorkItem(
                        _ =>
                        {
                            var context = ExecutionContext.Capture();
                            app(
                                env,
                                WrapResultDelegate(context, result),
                                fault);
                        },
                        null);
                    ExecutionContext.RestoreFlow();
                };
        }

        static ResultDelegate WrapResultDelegate(ExecutionContext context, ResultDelegate result)
        {
            return (status, headers, body) => result(
                status,
                headers,
                WrapBodyDelegate(context, body));
        }

        static BodyDelegate WrapBodyDelegate(ExecutionContext context, BodyDelegate body)
        {
            return body == null ? (BodyDelegate)null : (write, end, cancellationToken) => ExecutionContext.Run(
                context.CreateCopy(),
                _ => body(WrapWriteDelegate(context, write), end, cancellationToken),
                null);
        }

        static Func<ArraySegment<byte>, Action, bool> WrapWriteDelegate(ExecutionContext context, Func<ArraySegment<byte>, Action, bool> write)
        {
            return (data, callback) => callback == null ? write(data, null) : write(data, () => ExecutionContext.Run(context.CreateCopy(), _ => callback(), null));
        }
    }
}
