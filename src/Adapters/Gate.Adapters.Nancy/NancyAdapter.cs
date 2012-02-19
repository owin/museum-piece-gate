using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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
            return builder.Use<AppDelegate>(_ => App());
        }

        public static IAppBuilder RunNancy(this IAppBuilder builder, INancyBootstrapper bootstrapper)
        {
            return builder.Use<AppDelegate>(_ => App(bootstrapper));
        }

        public static AppDelegate App()
        {
            return App(NancyBootstrapperLocator.Bootstrapper);
        }

        public static AppDelegate App(INancyBootstrapper bootstrapper)
        {
            bootstrapper.Initialise();
            var engine = bootstrapper.GetEngine();
            return (env, result, fault) =>
            {
                Action<Exception> onError = ex =>
                {
                    fault(ex);
                };

                var owinRequestMethod = Get<string>(env, OwinConstants.RequestMethod);
                var owinRequestScheme = Get<string>(env, OwinConstants.RequestScheme);
                var owinRequestHeaders = Get<IDictionary<string, IEnumerable<string>>>(env, OwinConstants.RequestHeaders);
                var owinRequestPathBase = Get<string>(env, OwinConstants.RequestPathBase);
                var owinRequestPath = Get<string>(env, OwinConstants.RequestPath);
                var owinRequestQueryString = Get<string>(env, OwinConstants.RequestQueryString);
                var owinRequestBody = Get<BodyDelegate>(env, OwinConstants.RequestBody, EmptyBody);
                var cancellationToken = Get(env, typeof(CancellationToken).FullName, CancellationToken.None);
                var serverClientIp = Get<string>(env, "server.CLIENT_IP");
                var callDisposing = Get<CancellationToken>(env, "host.CallDisposing");

                var url = new Url
                {
                    Scheme = owinRequestScheme,
                    HostName = GetHeader(owinRequestHeaders, "Host"),
                    Port = null,
                    BasePath = owinRequestPathBase,
                    Path = owinRequestPath,
                    Query = owinRequestQueryString,
                };

                var body = new RequestStream(ExpectedLength(owinRequestHeaders), false);

                owinRequestBody.Invoke(
                    OnRequestData(body, onError),
                    _ => false,
                    readError =>
                    {
                        if (readError != null)
                        {
                            onError(readError);
                            return;
                        }

                        body.Position = 0;
                        var nancyRequest = new Request(owinRequestMethod, url, body, owinRequestHeaders, serverClientIp);

                        engine.HandleRequest(
                            nancyRequest,
                            context =>
                            {
                                callDisposing.Register(() =>
                                {
                                    context.Dispose();
                                });

                                var nancyResponse = context.Response;
                                var status = String.Format("{0} {1}", (int)nancyResponse.StatusCode, nancyResponse.StatusCode);
                                var headers = nancyResponse.Headers.ToDictionary(kv => kv.Key, kv => (IEnumerable<string>)new[] { kv.Value }, StringComparer.OrdinalIgnoreCase);
                                if (!string.IsNullOrWhiteSpace(nancyResponse.ContentType))
                                {
                                    headers["Content-Type"] = new[] { nancyResponse.ContentType };
                                }
                                if (nancyResponse.Cookies != null && nancyResponse.Cookies.Count != 0)
                                {
                                    headers["Set-Cookie"] = nancyResponse.Cookies.Select(cookie => cookie.ToString());
                                }

                                result(
                                    status,
                                    headers,
                                    (write, flush, end, cancel) =>
                                    {
                                        using (var stream = new ResponseStream(write, flush, end))
                                        {
                                            try
                                            {
                                                nancyResponse.Contents(stream);
                                            }
                                            catch (Exception ex)
                                            {
                                                stream.End(ex);
                                            }
                                        }
                                    });
                            },
                            onError);
                    },
                    cancellationToken);
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

        static string GetHeader(IDictionary<string, IEnumerable<string>> headers, string key)
        {
            IEnumerable<string> value;
            return headers.TryGetValue(key, out value) && value != null ? string.Join(",", value.ToArray()) : null;
        }

        static long ExpectedLength(IDictionary<string, IEnumerable<string>> headers)
        {
            var header = GetHeader(headers, "Content-Length");
            if (string.IsNullOrWhiteSpace(header))
                return 0;

            int contentLength;
            return int.TryParse(header, NumberStyles.Any, CultureInfo.InvariantCulture, out contentLength) ? contentLength : 0;
        }

        static void EmptyBody(Func<ArraySegment<byte>, bool> write, Func<Action, bool> flush, Action<Exception> end, CancellationToken cancellationToken)
        {
            end(null);
        }

        static Func<ArraySegment<byte>, bool> OnRequestData(Stream body, Action<Exception> fault)
        {
            return data =>
            {
                try
                {
                    body.Write(data.Array, data.Offset, data.Count);
                    return false;
                }
                catch (Exception ex)
                {
                    fault(ex);
                    return false;
                }
            };
        }
    }
}
