using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Owin;
using System.IO;

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
            return call =>
            {
                var owinRequestMethod = Get<string>(call.Environment, "owin.RequestMethod");
                var owinRequestScheme = Get<string>(call.Environment, "owin.RequestScheme");
                var owinRequestPath = Get<string>(call.Environment, "owin.RequestPath");
                var owinRequestPathBase = Get<string>(call.Environment, "owin.RequestPathBase");
                var owinRequestQueryString = Get<string>(call.Environment, "owin.RequestQueryString");
                var owinRequestProtocol = Get<string>(call.Environment, "owin.RequestProtocol");
                var owinRequestHeaders = call.Headers;
                var owinRequestBody = call.Body;
                var owinCallCompleted = Get<Task>(call.Environment, "owin.CallCompleted");

                var uriBuilder =
                    new UriBuilder(owinRequestScheme, "localhost")
                    {
                        Path = owinRequestPathBase + owinRequestPath,
                        Query = owinRequestQueryString
                    };

                var request = new HttpRequestMessage(new HttpMethod(owinRequestMethod), uriBuilder.Uri)
                {
                    Content = new StreamContent(owinRequestBody ?? Stream.Null)
                };
                request.Version = Version.Parse(owinRequestProtocol.Substring(5)); // HTTP/1.1

                if (owinRequestHeaders != null)
                {
                    foreach (var kv in owinRequestHeaders)
                    {
                        foreach (var value in kv.Value)
                        {
                            if (!request.Headers.TryAddWithoutValidation(kv.Key, kv.Value))
                            {
                                if (!request.Content.Headers.TryAddWithoutValidation(kv.Key, kv.Value))
                                {
                                    // TODO: Bad header name; Drop it (default), log it, or throw.
                                }
                            }
                        }
                    }
                }

                var cts = new CancellationTokenSource();
                owinCallCompleted.Finally(() => cts.Cancel(false));

                return invoker.SendAsync(request, cts.Token)
                    .Then(response =>
                    {
                        ResultParameters result = new ResultParameters();
                        result.Status = (int)response.StatusCode;
                        result.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        result.Properties.Add("owin.ReasonPhrase", response.ReasonPhrase);
                        result.Properties.Add("owin.ResponseProtocol", "HTTP/" + response.Version.ToString(2));

                        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headersEnumerable = response.Headers;
                        if (response.Content != null)
                            headersEnumerable = headersEnumerable.Concat(response.Content.Headers);

                        result.Headers = headersEnumerable.ToDictionary(
                            kv => kv.Key,
                            kv => kv.Value.ToArray(),
                            StringComparer.InvariantCultureIgnoreCase);

                        result.Body = GetResponseBody(response.Content);
                        return result;
                    });
            };
        }

        private static T Get<T>(IDictionary<string, object> env, string name)
        {
            object value;
            return env.TryGetValue(name, out value) && value is T ? (T)value : default(T);
        }


        private static Func<Stream, Task> GetResponseBody(HttpContent content)
        {
            if (content == null)
            {
                return null;
            }

            return output => content.CopyToAsync(output);
        }
    }
}
