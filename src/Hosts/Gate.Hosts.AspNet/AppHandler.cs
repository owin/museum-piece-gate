using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Owin;
using System.Diagnostics;

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
            var taskCompletionSource = new TaskCompletionSource<Action>(state);
            if (callback != null)
                taskCompletionSource.Task.ContinueWith(task => callback(task), TaskContinuationOptions.ExecuteSynchronously);

            var call = new CallParameters();

            var httpRequest = httpContext.Request;
            var serverVariables = new ServerVariables(httpRequest.ServerVariables);

            var pathBase = httpRequest.ApplicationPath;
            if (pathBase == "/" || pathBase == null)
                pathBase = "";

            var path = httpRequest.Path;
            if (path.StartsWith(pathBase))
                path = path.Substring(pathBase.Length);

            call.Headers = httpRequest.Headers.AllKeys
                .ToDictionary(x => x, x => httpRequest.Headers.GetValues(x), StringComparer.OrdinalIgnoreCase);

            call.Body = httpRequest.InputStream;

            call.Environment = new Dictionary<string, object>()
            { 
                {OwinConstants.Version, "1.0"},
                {OwinConstants.RequestMethod, httpRequest.HttpMethod},
                {OwinConstants.RequestScheme, httpRequest.Url.Scheme},
                {OwinConstants.RequestPathBase, pathBase},
                {OwinConstants.RequestPath, path},
                {OwinConstants.RequestQueryString, serverVariables.QueryString},
                {OwinConstants.RequestProtocol, serverVariables.ProtocolVersion},
                {"aspnet.HttpContextBase", httpContext},
                {OwinConstants.CallCompleted, taskCompletionSource.Task},
            };
            foreach (var kv in serverVariables.AddToEnvironment())
            {
                call.Environment["server." + kv.Key] = kv.Value;
            }

            try
            {
                _app.Invoke(call)
                    .Then(result =>
                    {
                        try
                        {
                            httpContext.Response.BufferOutput = false;

                            httpContext.Response.StatusCode = result.Status;
                            // TODO: Reason phrase
                            foreach (var header in result.Headers)
                            {
                                foreach (var value in header.Value)
                                {
                                    httpContext.Response.AddHeader(header.Key, value);
                                }
                            }

                            if (result.Body != null)
                            {
                                result.Body(httpContext.Response.OutputStream)
                                    .Then(() =>
                                    {
                                        taskCompletionSource.TrySetResult(() => { });
                                    })
                                    .Catch(errorInfo =>
                                    {
                                        taskCompletionSource.TrySetException(errorInfo.Exception);
                                        return errorInfo.Handled();
                                    });
                            }
                        }
                        catch (Exception ex)
                        {
                            taskCompletionSource.TrySetException(ex);
                        }
                    })
                    .Catch(errorInfo =>
                    {
                        taskCompletionSource.TrySetException(errorInfo.Exception);
                        return errorInfo.Handled();
                    });
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
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

            public string ProtocolVersion
            {
                get { return _serverVariables.Get("SERVER_PROTOCOL"); }
            }

            public IEnumerable<KeyValuePair<string, object>> AddToEnvironment()
            {
                return _serverVariables
                    .AllKeys
                    .Where(key => !key.StartsWith("HTTP_") && !string.Equals(key, "ALL_HTTP") && !string.Equals(key, "ALL_RAW"))
                    .Select(key => new KeyValuePair<string, object>(key, _serverVariables.Get(key)));
            }
        }
    }
}