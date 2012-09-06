using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Owin;
using System.Threading.Tasks;
using Gate.Middleware;
using System.IO;
using Gate.Middleware.Utils;

namespace Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class CascadeExtensions
    {        
        public static IAppBuilder UseCascade(this IAppBuilder builder, params AppFunc[] apps)
        {
            return builder.UseType<Cascade>(apps.AsEnumerable<AppFunc>());
        }
        
        public static IAppBuilder UseCascade(this IAppBuilder builder, params Action<IAppBuilder>[] apps)
        {
            return builder.UseType<Cascade>(apps.Select(cfg => builder.BuildNew<AppFunc>(x => cfg(x))));
        }
    }
}

namespace Gate.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    // This middleware helps an application create a forked pipeline. Each request is submitted to
    // a list of AppFuncs in order.  These apps either return a 404 response if they cannot
    // fulfill the request.  The cascade continues until an app returns a non-404 response, or the
    // the list of apps is exhausted.
    public class Cascade
    {
        private IEnumerable<AppFunc> apps;
        private AppFunc fallbackApp;
        
        public Cascade(AppFunc fallback, IEnumerable<AppFunc> apps)
        {
            this.apps = apps;
            this.fallbackApp = fallback;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            // the first non-404 result will the the one to take effect
            // any subsequent apps are not called

            var iter = apps.GetEnumerator();            

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            Stream outputStream = env.Get<Stream>(OwinConstants.ResponseBody);

            Action fallback = () => { };
            fallback = () =>
            {
                fallbackApp(env)
                    .Then(() => tcs.TrySetResult(null))
                    .Catch(errorInfo =>
                    {
                        tcs.TrySetException(errorInfo.Exception);
                        return errorInfo.Handled();
                    });
            };

            // Empty list
            if (!iter.MoveNext())
            {
                fallback();
                return tcs.Task;
            }

            Action loop = () => { };
            loop = () =>
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                for (var tryAgainOnSameThread = true; tryAgainOnSameThread; )
                {
                    TriggerStream triggerStream = new TriggerStream(outputStream);
                    triggerStream.OnFirstWrite = () =>
                    {
                        if (env.Get<int>(OwinConstants.ResponseStatusCode) == 404)
                        {
                            triggerStream.InnerStream = Stream.Null;
                        }
                    };

                    env[OwinConstants.ResponseBody] = triggerStream;

                    tryAgainOnSameThread = false;
                    iter.Current.Invoke(env)
                        .Then(() =>
                        {
                            if (env.Get<int>(OwinConstants.ResponseStatusCode) != 404)
                            {
                                tcs.TrySetResult(null);
                                return;
                            }

                            // Cleanup and try the next one.
                            env.Get<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders).Clear();
                            env[OwinConstants.ResponseBody] = outputStream;

                            if (iter.MoveNext())
                            {
                                // ReSharper disable AccessToModifiedClosure
                                if (threadId == Thread.CurrentThread.ManagedThreadId)
                                {
                                    tryAgainOnSameThread = true;
                                }
                                else
                                {
                                    loop();
                                }
                                // ReSharper restore AccessToModifiedClosure
                            }
                            else
                            {
                                fallback();
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
        }
    }
}