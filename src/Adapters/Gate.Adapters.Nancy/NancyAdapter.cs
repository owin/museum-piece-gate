using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Owin;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.IO;
using System.IO;

namespace Gate.Adapters.Nancy
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class NancyAdapter
    {
        public static IAppBuilder RunNancy(this IAppBuilder builder)
        {
            return builder.UseFunc<AppFunc>(_ => App());
        }

        public static IAppBuilder RunNancy(this IAppBuilder builder, INancyBootstrapper bootstrapper)
        {
            return builder.UseFunc<AppFunc>(_ => App(bootstrapper));
        }

        public static AppFunc App()
        {
            return App(NancyBootstrapperLocator.Bootstrapper);
        }

        public static AppFunc App(INancyBootstrapper bootstrapper)
        {
            bootstrapper.Initialise();
            var engine = bootstrapper.GetEngine();
            return env =>
            {
                var owinRequestMethod = Get<string>(env, OwinConstants.RequestMethod);
                var owinRequestScheme = Get<string>(env, OwinConstants.RequestScheme);
                var owinRequestHeaders = Get<IDictionary<string, string[]>>(env, OwinConstants.RequestHeaders);
                var owinRequestPathBase = Get<string>(env, OwinConstants.RequestPathBase);
                var owinRequestPath = Get<string>(env, OwinConstants.RequestPath);
                var owinRequestQueryString = Get<string>(env, OwinConstants.RequestQueryString);
                var owinRequestBody = Get<Stream>(env, OwinConstants.RequestBody);
                var serverClientIp = Get<string>(env, OwinConstants.RemoteIpAddress);

                var owinResponseHeaders = Get<IDictionary<string, string[]>>(env, OwinConstants.ResponseHeaders);
                var owinResponseBody = Get<Stream>(env, OwinConstants.ResponseBody);

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

                var tcs = new TaskCompletionSource<object>();
                engine.HandleRequest(
                    nancyRequest,
                    context =>
                    {
                        var nancyResponse = context.Response;
                        foreach (var header in nancyResponse.Headers)
                        {
                            owinResponseHeaders.Add(header.Key, new string[] { header.Value });
                        }

                        if (!string.IsNullOrWhiteSpace(nancyResponse.ContentType))
                        {
                            owinResponseHeaders["Content-Type"] = new[] { nancyResponse.ContentType };
                        }
                        if (nancyResponse.Cookies != null && nancyResponse.Cookies.Count != 0)
                        {
                            owinResponseHeaders["Set-Cookie"] = nancyResponse.Cookies.Select(cookie => cookie.ToString()).ToArray();
                        }

                        env[OwinConstants.ResponseStatusCode] = (int)nancyResponse.StatusCode;
                        nancyResponse.Contents(owinResponseBody);
                        context.Dispose();
                        tcs.TrySetResult(null);
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
