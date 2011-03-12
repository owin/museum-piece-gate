using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Gate.AspNet
{
    using AppDelegate = Action< // app
        IDictionary<string, object>, // env
        Action<Exception>, // fault
        Action< // result
            string, // status
            IDictionary<string, string>, // headers
            Func< // body
                Func< // next
                    ArraySegment<byte>, // data
                    Action, // continuation
                    bool>, // async                    
                Action<Exception>, // error
                Action, // complete
                Action>>>; // cancel


    public class GateModule : IHttpModule
    {
        public void Init(HttpApplication httpApplication)
        {
            AppDelegate app = (env, fault, result) => fault(new NotImplementedException());

            httpApplication.AddOnBeginRequestAsync(
                (sender, args, callback, state) =>
                {
                    var taskCompletionSource = new TaskCompletionSource<object>(state, TaskCreationOptions.PreferFairness);
                    if (callback != null)
                        taskCompletionSource.Task.ContinueWith(task => callback(task), TaskContinuationOptions.ExecuteSynchronously);

                    var httpContext = ((HttpApplication) sender).Context;

                    var env = new Dictionary<string, object> {{"", ""}};

                    app(
                        env,
                        taskCompletionSource.SetException,
                        (status, headers, body) =>
                        {
                            try
                            {
                                httpContext.Response.Status = status;
                                foreach (var header in headers.SelectMany(kv => kv.Value.Split("\r\n".ToArray(), StringSplitOptions.RemoveEmptyEntries).Select(v => new {kv.Key, Value = v})))
                                {
                                    httpContext.Response.AddHeader(header.Key, header.Value);
                                }
                                if (body == null)
                                {
                                    taskCompletionSource.SetResult(null);
                                    return;
                                }
                                var stream = httpContext.Response.OutputStream;
                                body(
                                    (data, continuation) =>
                                    {
                                        try
                                        {
                                            if (continuation == null)
                                            {
                                                stream.Write(data.Array, data.Offset, data.Count);
                                                return false;
                                            }
                                            var sr = stream.BeginWrite(data.Array, data.Offset, data.Count, ar =>
                                            {
                                                if (ar.CompletedSynchronously) return;
                                                try
                                                {
                                                    stream.EndWrite(ar);
                                                }
                                                catch (Exception ex)
                                                {
                                                    taskCompletionSource.SetException(ex);
                                                }
                                                continuation();
                                            }, null);
                                            if (sr.CompletedSynchronously)
                                            {
                                                stream.EndWrite(sr);
                                                return false;
                                            }

                                            return true;
                                        }
                                        catch (Exception ex)
                                        {
                                            taskCompletionSource.SetException(ex);
                                            return false;
                                        }
                                    },
                                    taskCompletionSource.SetException,
                                    () => taskCompletionSource.SetResult(null));
                            }
                            catch (Exception ex)
                            {
                                taskCompletionSource.SetException(ex);
                            }
                        });
                    return taskCompletionSource.Task;
                },
                ar => ((Task) ar).Wait());
        }

        public void Dispose()
        {
        }
    }
}