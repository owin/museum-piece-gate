using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Builder;
using Gate.Owin;
using Kayak;

namespace Gate.Kayak
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder RescheduleCallbacks(this IAppBuilder builder)
        {
            return builder.RescheduleCallbacks(null);
        }

        public static IAppBuilder RescheduleCallbacks(this IAppBuilder builder, IScheduler scheduler)
        {
            return builder.Use(del => new RescheduleCallbacksMiddleware(del, scheduler).Invoke);
        }
    }

    class RescheduleCallbacksMiddleware
    {
        AppDelegate wrapped;
        IScheduler scheduler;

        public RescheduleCallbacksMiddleware(AppDelegate wrapped, IScheduler scheduler)
        {
            this.wrapped = wrapped;
            this.scheduler = scheduler;
        }

        public void Invoke(IDictionary<string, object> envDict, ResultDelegate result, Action<Exception> error)
        {
            var env = (envDict as Environment ?? new Environment(envDict));
            var theScheduler = scheduler ?? (IScheduler)env["kayak.Scheduler"];

            var oldBody = env.Body;

            if (oldBody != null)
                env.Body = (onNext, onError, onComplete) =>
                {
                    theScheduler.Post(() =>
                    {
                        oldBody(
                            (data, ack) => onNext(data, ack == null ? (Action)ack : () => theScheduler.Post(ack)), 
                            onError, 
                            onComplete);
                    });

                    // XXX could properly provide this if the scheduler post above was hot.
                    // or could wait/block.
                    return () => { };
                };

            wrapped(env, (status, headers, body) => {
                theScheduler.Post(() => result(status, headers, (onNext, onError, onComplete) =>
                {
                    return body(
                        (data, _) => { 
                                    
                            // if the writer is async (continuation non-null)
                            // then we could keep track of the number of outstanding
                            // buffers and apply back pressure if it's > 0. invoke
                            // continuation when drained.

                            // XXX pool buffers

                            var buf = new byte[data.Count];
                            Buffer.BlockCopy(data.Array, data.Offset, buf, 0, buf.Length);
                            theScheduler.Post(() => onNext(new ArraySegment<byte>(buf), null));
                                    
                            return false;
                        },
                        bodyError => theScheduler.Post(() => onError(bodyError)),
                        () => theScheduler.Post(() => onComplete()));
                }));
            },
            e => {
                theScheduler.Post(() => error(e));
            });
        }
    }
}
