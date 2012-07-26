﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owin;
using Kayak;

// TODO: This whole component may be redundant now that Tasks are used.
/*
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

        public void Invoke(CallParameters call, Action<ResultParameters, Exception> callback)
        {
            var request = new RequestEnvironment(call.Environment);
            var theScheduler = scheduler ?? request.Scheduler;

            var oldBody = call.Body;

            if (oldBody != null)
            {
                call.Body = RescheduleBody(theScheduler, call.Body);
            }

            wrapped(
                call,
                (result, error) =>
                    theScheduler.Post(() =>
                    {
                        result.Body = RescheduleBody(theScheduler, result.Body);
                        callback(result, error);
                    }));

            //            result(status, headers, RescheduleBody(theScheduler, body))),
            //    e =>
            //        theScheduler.Post(() =>
            //            error(e)));
        }

        static BodyDelegate RescheduleBody(IScheduler theScheduler, BodyDelegate body)
        {
            // flush and end are tranported on theScheduler.
            // write is not, because you want the return value to be
            // false when the data is not buffering.

            return (write, end, cancel) =>
                theScheduler.Post(() =>
                    body(
                        (data, callback) =>
                        {
                            if (callback == null)
                            {
                                return write(data, callback);
                            }

                            theScheduler.Post(() =>
                            {
                                if (!write(data, () => { theScheduler.Post(callback); }))
                                    callback.Invoke();
                            });
                            return true;
                        },
                        ex => theScheduler.Post(() => end(ex)),
                        cancel));
        }
    }
}
*/