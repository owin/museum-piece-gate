using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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


    public class Module : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication init)
        {
            init.AddOnBeginRequestAsync(
                (sender, args, callback, state) =>
                {
                    var taskCompletionSource = new TaskCompletionSource<Action>(state);
                    if (callback != null)
                        taskCompletionSource.Task.ContinueWith(task => callback(task), TaskContinuationOptions.ExecuteSynchronously);

                    var httpContext = ((HttpApplication) sender).Context;
                    var httpRequest = httpContext.Request;
                    var serverVariables = new ServerVariables(httpRequest.ServerVariables);

                    var appRelCurExeFilPat = httpRequest.AppRelativeCurrentExecutionFilePath.Substring(1);

                    var env = new Dictionary<string, object>();
                    new Environment(env)
                    {
                        Version = "1.0",
                        Method = httpRequest.HttpMethod,
                        UriScheme = httpRequest.Url.Scheme,
                        ServerName = serverVariables.ServerName,
                        ServerPort = serverVariables.ServerPort,
                        BaseUri = "",
                        RequestUri = appRelCurExeFilPat + "?" + serverVariables.QueryString,
                        Headers = httpRequest.Headers.AllKeys.ToDictionary(x => x, x => httpRequest.Headers.Get(x)),
                        Body = (next, error, complete) =>
                        {
                            complete();
                            return () => { };
                        },
                    };
                    Host.Call(
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
                                    taskCompletionSource.SetResult(() => httpContext.Response.End());
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
                                    () => taskCompletionSource.SetResult(() => httpContext.Response.End()));
                            }
                            catch (Exception ex)
                            {
                                taskCompletionSource.SetException(ex);
                            }
                        });
                    return taskCompletionSource.Task;
                },
                ar => ((Task<Action>) ar).Result());
        }

        class ServerVariables
        {
            readonly NameValueCollection _serverVariables;

            public ServerVariables(NameValueCollection serverVariables)
            {
                _serverVariables = serverVariables;
            }

            public string QueryString
            {
                get { return _serverVariables.Get("QUERY_STRING"); }
            }

            public string ServerName
            {
                get { return _serverVariables.Get("SERVER_NAME"); }
            }

            public string ServerPort
            {
                get { return _serverVariables.Get("SERVER_PORT"); }
            }
        }
    }
}