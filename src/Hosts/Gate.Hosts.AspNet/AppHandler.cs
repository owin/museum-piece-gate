using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Owin;

namespace Gate.Hosts.AspNet
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
            var cancellationTokenSource = new CancellationTokenSource();
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

            var requestHeaders = httpRequest.Headers.AllKeys
                .ToDictionary(x => x, x => httpRequest.Headers.GetValues(x), StringComparer.OrdinalIgnoreCase);

            var env = new Dictionary<string, object>
            { 
                {OwinConstants.Version, "1.0"},
                {OwinConstants.RequestMethod, httpRequest.HttpMethod},
                {OwinConstants.RequestScheme, httpRequest.Url.Scheme},
                {OwinConstants.RequestPathBase, pathBase},
                {OwinConstants.RequestPath, path},
                {OwinConstants.RequestQueryString, serverVariables.QueryString},
                {OwinConstants.RequestHeaders, requestHeaders},
                {OwinConstants.RequestBody, RequestBody(httpRequest.InputStream)},
                {"host.CallDisposed", cancellationTokenSource.Token},
                {"aspnet.HttpContextBase", httpContext},
            };
            foreach (var kv in serverVariables.AddToEnvironment())
            {
                env["server." + kv.Key] = kv.Value;
            }

            try
            {
                _app.Invoke(
                    env,
                    (status, headers, body) =>
                    {
                        try
                        {
                            httpContext.Response.BufferOutput = false;

                            httpContext.Response.Status = status;
                            foreach (var header in headers)
                            {
                                foreach (var value in header.Value)
                                {
                                    httpContext.Response.AddHeader(header.Key, value);
                                }
                            }

                            ResponseBody(
                                body,
                                httpContext.Response.OutputStream,
                                ex => taskCompletionSource.TrySetException(ex),
                                () => taskCompletionSource.TrySetResult(() => { }));
                        }
                        catch (Exception ex)
                        {
                            taskCompletionSource.TrySetException(ex);
                        }
                    },
                    ex => taskCompletionSource.TrySetException(ex));
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
            }

            if (taskCompletionSource.Task.IsCompleted)
            {
                cancellationTokenSource.Cancel(false);
            }
            else
            {
                taskCompletionSource.Task.ContinueWith(t => cancellationTokenSource.Cancel(false));
            }

            return taskCompletionSource.Task;
        }

        public void EndProcessRequest(IAsyncResult asyncResult)
        {
            var task = ((Task<Action>)asyncResult);
            if (task.IsFaulted)
            {
                var exception = task.Exception;
                exception.Handle(ex => ex is HttpException);
            }
            else if (task.IsCompleted)
            {
                task.Result.Invoke();
            }
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

            public IEnumerable<KeyValuePair<string, object>> AddToEnvironment()
            {
                return _serverVariables
                    .AllKeys
                    .Where(key => !key.StartsWith("HTTP_") && !string.Equals(key, "ALL_HTTP") && !string.Equals(key, "ALL_RAW"))
                    .Select(key => new KeyValuePair<string, object>(key, _serverVariables.Get(key)));
            }
        }


        static BodyDelegate RequestBody(Stream stream)
        {
            return (write, end, cancellationToken) => new PipeRequest(stream, write, end, cancellationToken).Go();
        }

        static void ResponseBody(BodyDelegate body, Stream stream, Action<Exception> error, Action complete)
        {
            new PipeResponse(stream, error, complete).Go(body);
        }
    }
}