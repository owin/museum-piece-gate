using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using Gate.Builder;
using Gate.Owin;

namespace Gate.Hosts.HttpListener
{
    public class Server
    {
        public static IDisposable Create(int port)
        {
            return Create(port, "");
        }

        public static IDisposable Create(int port, string path)
        {
            return Create(ConfigurationManager.AppSettings["Gate.Startup"], port, path);
        }

        public static IDisposable Create(string startupName, int port)
        {
            return Create(startupName, port, "");
        }

        public static IDisposable Create(string startupName, int port, string path)
        {
            AppDelegate app = AppBuilder.BuildConfiguration(startupName);
            return Create(app, port, path);
        }

        public static IDisposable Create(AppDelegate app, int port)
        {
            return Create(app, port, "");
        }

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


                    var requestPathBase = effectivePath;
                    if (requestPathBase == "/" || requestPathBase == null)
                        requestPathBase = "";

                    var requestPath = context.Request.Url.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
                    if (string.IsNullOrEmpty(requestPath))
                        requestPath = "/";
                    if (requestPath.StartsWith(requestPathBase, StringComparison.OrdinalIgnoreCase))
                        requestPath = requestPath.Substring(requestPathBase.Length);

                    var requestQueryString = context.Request.Url.GetComponents(UriComponents.Query, UriFormat.UriEscaped);

                    var requestHeaders = context.Request.Headers.AllKeys
                        .ToDictionary(x => x, x => (IEnumerable<string>)context.Request.Headers.GetValues(x), StringComparer.OrdinalIgnoreCase);

                    var env = new Dictionary<string, object>
                    { 
                        {OwinConstants.Version, "1.0"},
                        {OwinConstants.RequestMethod, context.Request.HttpMethod},
                        {OwinConstants.RequestScheme, context.Request.Url.Scheme},
                        {OwinConstants.RequestPathBase, requestPathBase},
                        {OwinConstants.RequestPath, requestPath},
                        {OwinConstants.RequestQueryString, requestQueryString},
                        {OwinConstants.RequestHeaders, requestHeaders},
                        {OwinConstants.RequestBody, RequestBody(context.Request.InputStream)},
                        {"System.Net.HttpListenerContext", context},
                        {"server.CLIENT_IP", context.Request.RemoteEndPoint.Address.ToString()},
                    };

                    app(env,
                        (status, headers, body) =>
                        {
                            context.Response.StatusCode = int.Parse(status.Substring(0, 3));
                            context.Response.StatusDescription = status.Substring(4);
                            foreach (var kv in headers)
                            {
                                foreach (var v in kv.Value)
                                {
                                    context.Response.Headers.Add(kv.Key, v);
                                }
                            }
                            var pipeResponse = new PipeResponse(
                                context.Response.OutputStream,
                                ex => context.Response.Close(),
                                () => context.Response.Close());
                            pipeResponse.Go(body);
                        },
                        ex =>
                        {
                            // This should never be called
                            throw new NotImplementedException();
                        });
                },
                null);

            go();

            return new Disposable(() =>
            {
                go = () => { };
                listener.Stop();
            });
        }


        static BodyDelegate RequestBody(Stream stream)
        {
            return (write, flush, end, cancel) => new PipeRequest(stream, write, flush, end, cancel).Go();
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
