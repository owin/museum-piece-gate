using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Gate.Owin;

namespace Gate.HttpListener
{
    public class Server
    {
        public static IDisposable Create(AppDelegate app, int port)
        {
            return Create(app, port, "");
        }

        public static IDisposable Create(AppDelegate app, int port, string path)
        {
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


                    var requestPathBase = effectivePath;
                    if (requestPathBase == "/" || requestPathBase == null)
                        requestPathBase = "";

                    var requestPath = context.Request.Url.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
                    if (requestPath.StartsWith(requestPathBase, StringComparison.OrdinalIgnoreCase))
                        requestPath = requestPath.Substring(requestPathBase.Length);

                    var requestQueryString = context.Request.Url.GetComponents(UriComponents.Query, UriFormat.UriEscaped);

                    var requestHeaders = context.Request.Headers.AllKeys
                        .ToDictionary(x => x, x => context.Request.Headers.Get(x), StringComparer.OrdinalIgnoreCase);

                    var env = new Dictionary<string, object>
                    { 
                        {"owin.Version", "1.0"},
                        {"owin.RequestMethod", context.Request.HttpMethod},
                        {"owin.RequestScheme", context.Request.Url.Scheme},
                        {"owin.RequestPathBase", requestPathBase},
                        {"owin.RequestPath", requestPath},
                        {"owin.RequestQueryString", requestQueryString},
                        {"owin.RequestHeaders", context.Request.Headers},
                        {"owin.RequestBody", null},
                        {"System.Net.HttpListenerContext", context},
                        {"server.CLIENT_IP", context.Request.RemoteEndPoint.Address.ToString()},
                    };

                    try
                    {
                        app(env,
                            (status, headers, body) =>
                            {
                                context.Response.StatusCode = int.Parse(status.Substring(0, 3));
                                context.Response.StatusDescription = status.Substring(4);
                                foreach (var kv in headers)
                                {
                                    foreach (var v in kv.Value.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        context.Response.Headers.Add(kv.Key, v);
                                    }
                                }
                                var pipeResponse = new PipeResponse(
                                    context.Response.OutputStream,
                                    ex => { context.Response.Close(); },
                                    () => { context.Response.Close(); });
                                pipeResponse.Go(body);
                            },
                            ex =>
                            {
                                context.Response.StatusCode = 500;
                                context.Response.StatusDescription = "Server Error";
                                context.Response.Close();
                            });
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.StatusDescription = "Server Error";
                        context.Response.Close();
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
