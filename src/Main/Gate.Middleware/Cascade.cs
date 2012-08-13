﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Owin;
using System.Threading.Tasks;

namespace Gate.Middleware
{
    public static class Cascade
    {
        public static void RunCascade(this IAppBuilder builder, params AppDelegate[] apps)
        {
            builder.Run(App(apps));
        }

        public static void RunCascade(this IAppBuilder builder, params Action<IAppBuilder>[] apps)
        {
            builder.Run(App(apps.Select(cfg => builder.BuildNew<AppDelegate>(x => cfg(x)))));
        }

        public static IAppBuilder UseCascade(this IAppBuilder builder, params AppDelegate[] apps)
        {
            return builder.UseFunc<AppDelegate>(app => Middleware(app, apps));
        }

        public static IAppBuilder UseCascade(this IAppBuilder builder, params Action<IAppBuilder>[] apps)
        {
            return builder.UseFunc<AppDelegate>(app => Middleware(app, apps.Select(cfg => builder.BuildNew<AppDelegate>(x => cfg(x)))));
        }


        public static AppDelegate App(IEnumerable<AppDelegate> apps)
        {
            return Middleware(null, apps);
        }
        public static AppDelegate App(AppDelegate app0)
        {
            return Middleware(null, new[] { app0 });
        }
        public static AppDelegate App(AppDelegate app0, AppDelegate app1)
        {
            return Middleware(null, new[] { app0, app1 });
        }
        public static AppDelegate App(AppDelegate app0, AppDelegate app1, AppDelegate app2)
        {
            return Middleware(null, new[] { app0, app1, app2 });
        }

        public static AppDelegate Middleware(AppDelegate app, AppDelegate app0)
        {
            return Middleware(app, new[] { app0 });
        }
        public static AppDelegate Middleware(AppDelegate app, AppDelegate app0, AppDelegate app1)
        {
            return Middleware(app, new[] { app0, app1 });
        }
        public static AppDelegate Middleware(AppDelegate app, AppDelegate app0, AppDelegate app1, AppDelegate app2)
        {
            return Middleware(app, new[] { app0, app1, app2 });
        }

        public static AppDelegate Middleware(AppDelegate app, IEnumerable<AppDelegate> apps)
        {
            // sequence to attempt is {apps[0], apps[n], app}
            // or {apps[0], apps[n]} if app is null
            apps = (apps ?? new AppDelegate[0]).Concat(new[] { app ?? NotFound.Call }).ToArray();

            // the first non-404 result will the the one to take effect
            // any subsequent apps are not called
            return call =>
            {
                var iter = apps.GetEnumerator();
                iter.MoveNext();

                TaskCompletionSource<ResultParameters> tcs = new TaskCompletionSource<ResultParameters>();

                Action loop = () => { };
                loop = () =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    for (var hot = true; hot; )
                    {
                        hot = false;
                        iter.Current.Invoke(call)
                            .Then(result =>
                            {
                                if (result.Status == 404 && iter.MoveNext())
                                {
                                    // ReSharper disable AccessToModifiedClosure
                                    if (threadId == Thread.CurrentThread.ManagedThreadId)
                                    {
                                        hot = true;
                                    }
                                    else
                                    {
                                        loop();
                                    }
                                    // ReSharper restore AccessToModifiedClosure
                                }
                                else
                                {
                                    tcs.TrySetResult(result);
                                }
                            })
                            .Catch(errorInfo =>
                            {
                                tcs.TrySetException(errorInfo.Exception);
                                return errorInfo.Handled();
                            });
                    }
                    threadId = 0;
                };

                loop();

                return tcs.Task;
            };
        }
    }
}