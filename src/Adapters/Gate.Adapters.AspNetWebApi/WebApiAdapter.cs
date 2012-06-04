using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Owin;

namespace Gate.Adapters.AspNetWebApi
{
    public static class WebApiAdapter
    {
        public static IAppBuilder RunHttpServer(this IAppBuilder builder)
        {
            return builder.Use<AppDelegate>(_ => App());
        }

        public static IAppBuilder RunHttpServer(this IAppBuilder builder, HttpControllerDispatcher dispatcher)
        {
            return builder.Use<AppDelegate>(_ => App(dispatcher));
        }

        public static IAppBuilder RunHttpServer(this IAppBuilder builder, HttpConfiguration configuration)
        {
            return builder.Use<AppDelegate>(_ => App(configuration));
        }

        public static IAppBuilder RunHttpServer(this IAppBuilder builder, HttpConfiguration configuration, HttpMessageHandler dispatcher)
        {
            return builder.Use<AppDelegate>(_ => App(configuration, dispatcher));
        }

        public static IAppBuilder RunHttpServer(this IAppBuilder builder, HttpServer server)
        {
            return builder.Use<AppDelegate>(_ => App(server));
        }

        public static AppDelegate App()
        {
            return App(new HttpServer());
        }
        public static AppDelegate App(HttpConfiguration configuration)
        {
            return App(new HttpServer(configuration));
        }
        public static AppDelegate App(HttpConfiguration configuration, HttpMessageHandler dispatcher)
        {
            return App(new HttpServer(configuration, dispatcher));
        }
        public static AppDelegate App(HttpControllerDispatcher dispatcher)
        {
            return App(new HttpServer(dispatcher));
        }

        public static AppDelegate App(HttpServer server)
        {
            var invoker = new HttpMessageInvoker(server);
            return (env, result, fault) =>
            {
                var owinRequestMethod = Get<string>(env, "owin.RequestMethod");
                var owinRequestPath = Get<string>(env, "owin.RequestPath");
                var owinRequestPathBase = Get<string>(env, "owin.RequestPathBase");
                var owinRequestQueryString = Get<string>(env, "owin.RequestQueryString");
                var owinRequestHeaders = Get<IDictionary<string, IEnumerable<string>>>(env, "owin.RequestHeaders");
                var owinRequestBody = Get<BodyDelegate>(env, "owin.RequestBody");
                var owinRequestScheme = Get<string>(env, "owin.RequestScheme");
                var cancellationToken = Get<CancellationToken>(env, "System.Threading.CancellationToken");

                var uriBuilder =
                    new UriBuilder(owinRequestScheme, "localhost")
                    {
                        Path = owinRequestPathBase + owinRequestPath,
                        Query = owinRequestQueryString
                    };

                var request = new HttpRequestMessage(new HttpMethod(owinRequestMethod), uriBuilder.Uri)
                {
                    Content = new RequestHttpContent(owinRequestBody, cancellationToken)
                };

                if (owinRequestHeaders != null)
                {
                    foreach (var kv in owinRequestHeaders)
                    {
                        foreach (var value in kv.Value)
                        {
                            if (kv.Key.StartsWith("content", StringComparison.InvariantCultureIgnoreCase))
                            {
                                request.Content.Headers.Add(kv.Key, value);
                            }
                            else
                            {
                                request.Headers.Add(kv.Key, value);
                            }
                        }
                    }
                }
                
                invoker.SendAsync(request, cancellationToken)
                    .Then(response =>
                    {
                        var status = (int)response.StatusCode + " " + response.ReasonPhrase;

                        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headersEnumerable = response.Headers;
                        if (response.Content != null)
                            headersEnumerable = headersEnumerable.Concat(response.Content.Headers);

                        var headers = headersEnumerable.ToDictionary(
                            kv => kv.Key,
                            kv => kv.Value.ToArray(),
                            StringComparer.InvariantCultureIgnoreCase);

                        result(status, headers, ResponseBody(response.Content));
                    })
                    .Catch(fault);
            };
        }

        private static T Get<T>(IDictionary<string, object> env, string name)
        {
            object value;
            return env.TryGetValue(name, out value) && value is T ? (T)value : default(T);
        }


        private static BodyDelegate ResponseBody(HttpContent content)
        {
            if (content == null)
            {
                return (write, end, cancel) => end(null);
            }

            return (write, end, cancel) =>
            {
                try
                {
                    var stream = new ResponseHttpStream(write, end);
                    content.CopyToAsync(stream)
                        .Then(() =>
                        {
                            stream.Close();
                            end(null);
                        })
                        .Catch(ex =>
                        {
                            stream.Close();
                            end(ex);
                        });
                }
                catch (Exception ex)
                {
                    end(ex);
                }
            };
        }
    }
}
