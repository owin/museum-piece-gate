using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Owin;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.IO;

namespace Gate.Adapters.Nancy
{
    public static class NancyAdapter
    {
        public static IAppBuilder RunNancy(this IAppBuilder builder)
        {
            return builder.UseFunc<AppDelegate>(_ => App());
        }

        public static IAppBuilder RunNancy(this IAppBuilder builder, INancyBootstrapper bootstrapper)
        {
            return builder.UseFunc<AppDelegate>(_ => App(bootstrapper));
        }

        public static AppDelegate App()
        {
            return App(NancyBootstrapperLocator.Bootstrapper);
        }

        public static AppDelegate App(INancyBootstrapper bootstrapper)
        {
            bootstrapper.Initialise();
            var engine = bootstrapper.GetEngine();
            return call =>
            {
                var env = call.Environment;

                var owinRequestMethod = Get<string>(env, OwinConstants.RequestMethod);
                var owinRequestScheme = Get<string>(env, OwinConstants.RequestScheme);
                var owinRequestHeaders = call.Headers;
                var owinRequestPathBase = Get<string>(env, OwinConstants.RequestPathBase);
                var owinRequestPath = Get<string>(env, OwinConstants.RequestPath);
                var owinRequestQueryString = Get<string>(env, OwinConstants.RequestQueryString);
                var owinRequestBody = call.Body;
                var serverClientIp = Get<string>(env, "server.RemoteIpAddress");
                var callCompleted = Get<Task>(env, OwinConstants.CallCompleted);

                var url = new Url
                {
                    Scheme = owinRequestScheme,
                    HostName = GetHeader(owinRequestHeaders, "Host"),
                    Port = null,
                    BasePath = owinRequestPathBase,
                    Path = owinRequestPath,
                    Query = owinRequestQueryString,
                };

                var body = new RequestStream(owinRequestBody, ExpectedLength(owinRequestHeaders), false);

                var nancyRequest = new Request(
                    owinRequestMethod,
                    url,
                    body,
                    owinRequestHeaders.ToDictionary(kv => kv.Key, kv => (IEnumerable<string>)kv.Value, StringComparer.OrdinalIgnoreCase),
                    serverClientIp);

                var tcs = new TaskCompletionSource<ResultParameters>();
                engine.HandleRequest(
                    nancyRequest,
                    context =>
                    {
                        callCompleted.Finally(context.Dispose);

                        var nancyResponse = context.Response;
                        var headers = nancyResponse.Headers.ToDictionary(kv => kv.Key, kv => new[] { kv.Value }, StringComparer.OrdinalIgnoreCase);
                        if (!string.IsNullOrWhiteSpace(nancyResponse.ContentType))
                        {
                            headers["Content-Type"] = new[] { nancyResponse.ContentType };
                        }
                        if (nancyResponse.Cookies != null && nancyResponse.Cookies.Count != 0)
                        {
                            headers["Set-Cookie"] = nancyResponse.Cookies.Select(cookie => cookie.ToString()).ToArray();
                        }
                        tcs.SetResult(new ResultParameters
                        {
                            Status = (int)nancyResponse.StatusCode,
                            Headers = headers,
                            Body = output =>
                            {
                                nancyResponse.Contents(output);
                                return TaskHelpers.Completed();
                            }
                        });
                    },
                    tcs.SetException);
                return tcs.Task;
            };
        }


        static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            return env.TryGetValue(key, out value) && value is T ? (T)value : default(T);
        }

        static T Get<T>(IDictionary<string, object> env, string key, T defaultValue)
        {
            object value;
            return env.TryGetValue(key, out value) && value is T ? (T)value : defaultValue;
        }

        static string GetHeader(IDictionary<string, string[]> headers, string key)
        {
            string[] value;
            return headers.TryGetValue(key, out value) && value != null ? string.Join(",", value.ToArray()) : null;
        }

        static long ExpectedLength(IDictionary<string, string[]> headers)
        {
            var header = GetHeader(headers, "Content-Length");
            if (string.IsNullOrWhiteSpace(header))
                return 0;

            int contentLength;
            return int.TryParse(header, NumberStyles.Any, CultureInfo.InvariantCulture, out contentLength) ? contentLength : 0;
        }

    }
}
