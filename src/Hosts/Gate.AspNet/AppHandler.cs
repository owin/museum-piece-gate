using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Gate.AspNet
{
    public class AppHandler
    {
        readonly AppDelegate _app;

        public AppHandler(AppDelegate app)
        {
            _app = app;
        }

        public IAsyncResult BeginProcessRequest(HttpContextBase httpContext, AsyncCallback callback, object state)
        {
            var taskCompletionSource = new TaskCompletionSource<Action>(state);
            if (callback != null)
                taskCompletionSource.Task.ContinueWith(task => callback(task), TaskContinuationOptions.ExecuteSynchronously);

            var httpRequest = httpContext.Request;
            var serverVariables = new ServerVariables(httpRequest.ServerVariables);

            var pathBase = httpRequest.ApplicationPath;
            if (pathBase == "/" || pathBase == null)
                pathBase = "";

            var path = httpRequest.Path;
            if (path.StartsWith(pathBase))
                path = path.Substring(pathBase.Length);

            var env = new Dictionary<string, object>();
            new Environment(env)
            {
                Version = "1.0",
                Method = httpRequest.HttpMethod,
                Scheme = httpRequest.Url.Scheme,
                PathBase = pathBase,
                Path = path,
                QueryString = serverVariables.QueryString,
                Headers = httpRequest.Headers.AllKeys.ToDictionary(x => x, x => httpRequest.Headers.Get(x)),
                Body = (Func<ArraySegment<byte>, Action, bool> next, Action<Exception> error, Action complete) =>
                {
                    var stream = httpContext.Request.InputStream;
                    var buffer = new byte[4096];
                    var continuation = new AsyncCallback[1];
                    bool[] stopped = {false};
                    continuation[0] = result =>
                    {
                        if (result != null && result.CompletedSynchronously) return;
                        try
                        {
                            for (;;)
                            {
                                if (result != null)
                                {
                                    var count = stream.EndRead(result);
                                    if (stopped[0]) return;
                                    if (count <= 0)
                                    {
                                        complete();
                                        return;
                                    }
                                    var data = new ArraySegment<byte>(buffer, 0, count);
                                    if (next(data, () => continuation[0](null))) return;
                                }

                                if (stopped[0]) return;
                                result = stream.BeginRead(buffer, 0, buffer.Length, continuation[0], null);
                                if (!result.CompletedSynchronously) return;
                            }
                        }
                        catch (Exception ex)
                        {
                            error(ex);
                        }
                    };
                    continuation[0](null);
                    return () => { stopped[0] = true; };
                },
            };
            _app.Invoke(
                env,
                (status, headers, body) =>
                {
                    try
                    {
                        httpContext.Response.BufferOutput = false;

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
                },
                taskCompletionSource.SetException);
            return taskCompletionSource.Task;
        }

        public void EndProcessRequest(IAsyncResult asyncResult)
        {
            var task = ((Task<Action>) asyncResult);
            task.Result.Invoke();
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