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
            return body == null ? (BodyDelegate)null : (write, flush, end, cancellationToken) => ExecutionContext.Run(
                context.CreateCopy(),
                _ => body(write, WrapFlushDelegate(context, flush), end, cancellationToken),
                null);
        }

        static Func<Action, bool> WrapFlushDelegate(ExecutionContext context, Func<Action, bool> flush)
        {
            return drained => drained == null ? flush(null) : flush(() => ExecutionContext.Run(context.CreateCopy(), _ => drained(), null));
        }
    }
}
