using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Builder;
using Gate.Owin;
using Kayak;

namespace Gate.Hosts.Kayak
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder RescheduleCallbacks(this IAppBuilder builder)
        {
            return builder.RescheduleCallbacks(null);
        }

        public static IAppBuilder RescheduleCallbacks(this IAppBuilder builder, IScheduler scheduler)
        {
            return builder.Use<AppDelegate>(app => new RescheduleCallbacksMiddleware(app, scheduler).Invoke);
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

        public void Invoke(IDictionary<string, object> env, ResultDelegate result, Action<Exception> error)
        {
            var request = new RequestEnvironment(env);
            var theScheduler = scheduler ?? request.Scheduler;

            var oldBody = request.BodyDelegate;

            if (oldBody != null)
                request.BodyDelegate = (onNext, onError, onComplete) =>
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
                        (data, continuation) =>
                        {
                            // cannot reschedule a call that is sent synchronously...
                            if (continuation == null)
                            {
                                onNext(data, null);
                                return false;
                            }

                            // reschedule any async calls, and call continuation if host does not
                            theScheduler.Post(() =>
                            {
                                if (!onNext(data, continuation))
                                    continuation();
                            });

                            return true;
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
