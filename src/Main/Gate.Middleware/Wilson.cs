using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Owin;
using Timer = System.Timers.Timer;

namespace Gate.Middleware
{
    public class Wilson
    {
        public static AppDelegate App(bool asyncReply)
        {
            return asyncReply ? AsyncApp() : App();
        }

        public static AppDelegate App()
        {
            return call =>
            {
                var request = new Request(call);
                var response = new Response { ContentType = "text/html" };
                var wilson = "left - right\r\n123456789012\r\nhello world!\r\n";

                var href = "?flip=left";
                if (request.Query["flip"] == "left")
                {
                    wilson = wilson.Split(new[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => new string(line.Reverse().ToArray()))
                        .Aggregate("", (agg, line) => agg + line + System.Environment.NewLine);
                    href = "?flip=right";
                }
                response.Body.Write("<title>Wilson</title>");
                response.Body.Write("<pre>");
                response.Body.Write(wilson);
                response.Body.Write("</pre>");
                if (request.Query["flip"] == "crash")
                {
                    throw new ApplicationException("Wilson crashed!");
                }
                response.Body.Write("<p><a href='" + href + "'>flip!</a></p>");
                response.Body.Write("<p><a href='?flip=crash'>crash!</a></p>");

                return response.EndAsync();
            };
        }

        public static AppDelegate AsyncApp()
        {
            return call =>
            {
                var request = new Request(call);
                var response = new Response()
                {
                    ContentType = "text/html",
                };
                var wilson = "left - right\r\n123456789012\r\nhello world!\r\n";

                response.Body = new ResponseBody(
                    body =>
                    {
                        var href = "?flip=left";
                        if (request.Query["flip"] == "left")
                        {
                            wilson = wilson.Split(new[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(line => new string(line.Reverse().ToArray()))
                                .Aggregate("", (agg, line) => agg + line + System.Environment.NewLine);
                            href = "?flip=right";
                        }

                        return TimerLoop(350,
                            () => body.Write("<title>Hutchtastic</title>"),
                            () => body.Write("<pre>"),
                            () => body.Write(wilson),
                            () => body.Write("</pre>"),
                            () =>
                            {
                                if (request.Query["flip"] == "crash")
                                {
                                    throw new ApplicationException("Wilson crashed!");
                                }
                            },
                            () => body.Write("<p><a href='" + href + "'>flip!</a></p>"),
                            () => body.Write("<p><a href='?flip=crash'>crash!</a></p>"));
                    });

                return response.EndAsync();
            };
        }

        static Task TimerLoop(double interval, params Action[] steps)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var iter = steps.AsEnumerable().GetEnumerator();
            var timer = new Timer(interval);
            timer.Elapsed += (sender, e) =>
            {
                if (iter != null && iter.MoveNext())
                {
                    try
                    {
                        iter.Current();
                    }
                    catch (Exception ex)
                    {
                        iter = null;
                        timer.Stop();
                        tcs.TrySetException(ex);
                    }
                }
                else
                {
                    tcs.TrySetResult(null);
                    timer.Stop();
                }
            };
            timer.Start();
            return tcs.Task;
        }
    }
}