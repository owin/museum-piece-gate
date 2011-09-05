using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Gate.Helpers
{
    public class Cascade : IApplication<IEnumerable<AppDelegate>>
    {
        AppDelegate IApplication<IEnumerable<AppDelegate>>.Create(IEnumerable<AppDelegate> apps)
        {
            return Create(apps);
        }

        public static AppDelegate Try(AppDelegate fallback, AppDelegate app1)
        {
            return Create(new[] { app1, fallback});
        }
        public static AppDelegate Try(AppDelegate fallback, AppDelegate app1, AppDelegate app2)
        {
            return Create(new[] { app1, app2, fallback });
        }

        public static AppDelegate Create(params AppDelegate[] apps)
        {
            return Create((IEnumerable<AppDelegate>) apps);
        }

        public static AppDelegate Create(IEnumerable<AppDelegate> apps)
        {
            if (apps == null || !apps.Any())
                apps = new[] {NotFound.Create()};

            return (env, result, fault) =>
            {
                var iter = apps.GetEnumerator();
                iter.MoveNext();

                Action loop = () => { };
                loop = () =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    for (var hot = true; hot;)
                    {
                        hot = false;
                        iter.Current.Invoke(
                            env,
                            (status, headers, body) =>
                            {
                                try
                                {
                                    if (status.StartsWith("404") && iter.MoveNext())
                                    {
                                        if (threadId == Thread.CurrentThread.ManagedThreadId)
                                            hot = true;
                                        else
                                            loop();
                                    }
                                    else
                                    {
                                        result(status, headers, body);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    fault(ex);
                                }
                            },
                            fault);
                    }
                    threadId = 0;
                };

                loop();
            };
        }
    }
}