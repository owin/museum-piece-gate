using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owin;
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
            {
                request.BodyDelegate = RescheduleBody(theScheduler, request.BodyDelegate);
            }
            wrapped(
                env,
                (status, headers, body) =>
                    theScheduler.Post(() =>
                        result(status, headers, RescheduleBody(theScheduler, body))),
                e =>
                    theScheduler.Post(() =>
                        error(e)));
        }

        static BodyDelegate RescheduleBody(IScheduler theScheduler, BodyDelegate body)
        {
            // flush and end are tranported on theScheduler.
            // write is not, because you want the return value to be
            // false when the data is not buffering.

            return (write, flush, end, cancel) =>
                theScheduler.Post(() =>
                    body(
                        write,
                        drained =>
                        {
                            theScheduler.Post(() =>
                            {
                                if (!flush(() => if (drained != null) theScheduler.Post(drained)))
                                    if (drained != null) drained.Invoke();
                            });
                            return true;
                        },
                        ex => theScheduler.Post(() => end(ex)),
                        cancel));
        }
    }
}
