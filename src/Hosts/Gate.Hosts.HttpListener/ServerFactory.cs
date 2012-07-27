using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Gate.Hosts.HttpListener;
using Owin;
using System.Threading.Tasks;
using System.Threading;

[assembly: ServerFactory]

namespace Gate.Hosts.HttpListener
{
    public class ServerFactory : Attribute
    {
        public static IDisposable Create(AppDelegate app, int port, string path)
        {
            app = ErrorPage.Middleware(app);

            var effectivePath = path ?? "";
            if (!effectivePath.EndsWith("/"))
                effectivePath += "/";

            var listener = new System.Net.HttpListener();
            listener.Prefixes.Add(string.Format("http://+:{0}{1}", port, effectivePath));
            listener.Start();

            Action go = () => { };
            go = () => listener.BeginGetContext(
                ar =>
                {
                    HttpListenerContext context;
                    try
                    {
                        context = listener.EndGetContext(ar);
                    }
                    finally
                    {
                        // ReSharper disable AccessToModifiedClosure
                        go();
                        // ReSharper restore AccessToModifiedClosure
                    }

                    CallParameters call;

                    var requestPathBase = effectivePath;
                    if (requestPathBase == "/" || requestPathBase == null)
                        requestPathBase = "";

                    var requestPath = context.Request.Url.AbsolutePath;
                    if (string.IsNullOrEmpty(requestPath))
                        requestPath = "/";
                    if (requestPath.StartsWith(requestPathBase, StringComparison.OrdinalIgnoreCase))
                        requestPath = requestPath.Substring(requestPathBase.Length);

                    var requestQueryString = context.Request.Url.GetComponents(UriComponents.Query, UriFormat.UriEscaped);

                    call.Headers = context.Request.Headers.AllKeys
                        .ToDictionary(x => x, x => context.Request.Headers.GetValues(x), StringComparer.OrdinalIgnoreCase);

                    call.Environment = new Dictionary<string, object>
                    { 
                        {OwinConstants.Version, "1.0"},
                        {OwinConstants.RequestMethod, context.Request.HttpMethod},
                        {OwinConstants.RequestScheme, context.Request.Url.Scheme},
                        {OwinConstants.RequestPathBase, requestPathBase},
                        {OwinConstants.RequestPath, requestPath},
                        {OwinConstants.RequestQueryString, requestQueryString},
                        {"System.Net.HttpListenerContext", context},
                    };

                    call.Body = context.Request.InputStream;

                    try
                    {
                        Task<ResultParameters> appTask = app(call);
                        
                        // No real error handling, just close the connection.
                        appTask.ContinueWith(task => context.Response.Abort(), TaskContinuationOptions.NotOnRanToCompletion);

                        // Success
                        appTask.Then(
                            result =>
                            {
                                context.Response.StatusCode = result.Status;
                                // context.Response.StatusDescription = ;
                                foreach (var kv in result.Headers)
                                {
                                    // these may not be assigned via header collection
                                    if (string.Equals(kv.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                                    {
                                        context.Response.ContentLength64 = long.Parse(kv.Value.Single());
                                    }
                                    else if (string.Equals(kv.Key, "Keep-Alive", StringComparison.OrdinalIgnoreCase))
                                    {
                                        context.Response.KeepAlive = true;
                                    }
                                    //else if (string.Equals(kv.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    // not sure what can be done about this
                                    //}
                                    //else if (string.Equals(kv.Key, "WWW-Authenticate", StringComparison.OrdinalIgnoreCase))
                                    //{
                                    //    // not sure what httplistener properties to assign                                    
                                    //}
                                    else
                                    {
                                        // all others are
                                        foreach (var v in kv.Value)
                                        {
                                            context.Response.Headers.Add(kv.Key, v);
                                        }
                                    }
                                }

                                if (result.Body != null)
                                {
                                    try
                                    {
                                        Task bodyTask = result.Body(context.Response.OutputStream);

                                        bodyTask.ContinueWith(task => context.Response.Abort(), TaskContinuationOptions.NotOnRanToCompletion);

                                        bodyTask.Then(() => context.Response.Close());
                                    }
                                    catch (Exception)
                                    {
                                        context.Response.Abort();
                                    }
                                }
                            });

                    }
                    catch (Exception)
                    {
                        context.Response.Abort();
                    }
                },
                null);

            go();

            return new Disposable(() =>
            {
                go = () => { };
                listener.Stop();
            });
        }

        public class Disposable : IDisposable
        {
            readonly Action _dispose;

            public Disposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose();
            }
        }

    }
}
