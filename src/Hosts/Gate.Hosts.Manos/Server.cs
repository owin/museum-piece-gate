using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using Gate.Builder;
using Owin;
using Manos.Collections;
using Manos.Http;
using Manos.IO;

namespace Gate.Hosts.Manos
{
    public static class Server
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

            var effectivePath = path;

            var endpoint = new IPEndPoint(IPAddress.Any, port);
            var context = Context.Create();
            var httpServer = new HttpServer(
                context,
                transaction =>
                {
                    var cts = new CancellationTokenSource();

                    var requestPathBase = effectivePath;
                    if (requestPathBase == "/" || requestPathBase == null)
                        requestPathBase = "";

                    var requestPath = transaction.Request.Path;
                    if (requestPath.StartsWith(requestPathBase, StringComparison.OrdinalIgnoreCase))
                        requestPath = requestPath.Substring(requestPathBase.Length);


                    var requestQueryString = RequestQueryString(transaction.Request.QueryData);

                    var requestHeaders = transaction.Request.Headers.Keys.ToDictionary(k => k, k => transaction.Request.Headers[k], StringComparer.OrdinalIgnoreCase);
                    var env = new Dictionary<string, object>
                    { 
                        {OwinConstants.Version, "1.0"},
                        {OwinConstants.RequestMethod, transaction.Request.Method.ToString().Substring(5)},
                        {OwinConstants.RequestScheme, "http"},
                        {OwinConstants.RequestPathBase, requestPathBase},
                        {OwinConstants.RequestPath, requestPath},
                        {OwinConstants.RequestQueryString, requestQueryString},
                        {OwinConstants.RequestHeaders, requestHeaders},
                        {OwinConstants.RequestBody, RequestBody(transaction.Request.PostBody, transaction.Request.ContentEncoding)},
                        {"Manos.Http.IHttpTransaction", transaction},
                        {"server.CLIENT_IP", transaction.Request.Socket.RemoteEndpoint.Address.ToString()},
                        {"System.Threading.CancellationToken", cts.Token}
                    };

                    app(
                        env,
                        (status, headers, body) =>
                        {
                            transaction.Response.StatusCode = int.Parse(status.Substring(0, 3));
                            foreach (var header in headers)
                            {
                                if (string.Equals(header.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
                                {
                                    // use a header-injection to avoid re-parsing values into Manos HttpCookie structure
                                    transaction.Response.SetHeader(header.Key, string.Join("\r\nSet-Cookie: ", header.Value.ToArray()));
                                }
                                else
                                {
                                    transaction.Response.SetHeader(header.Key, string.Join(",", header.Value.ToArray()));
                                }
                            }

                            body(
                                data =>
                                {
                                    var duplicate = new byte[data.Count];
                                    Array.Copy(data.Array, data.Offset, duplicate, 0, data.Count);
                                    transaction.Response.Write(duplicate);
                                    return false;
                                },
                                _ => false,
                                ex => transaction.Response.End(),
                                cts.Token);
                        },
                        ex =>
                        {
                            // This should never be called
                            throw new NotImplementedException();
                        });
                },
                context.CreateTcpServerSocket(endpoint.AddressFamily),
                true);
            httpServer.Listen(endpoint.Address.ToString(), port);

            var thread = new Thread(context.Start);
            thread.Start();


            return new Disposable(() =>
            {
                context.Stop();
                thread.Join(250);
                //httpServer.Dispose();
            });
        }

        static string RequestQueryString(DataDictionary queryData)
        {
            var count = queryData.Count;
            if (count == 0)
                return null;

            var sb = new StringBuilder();
            foreach (var key in queryData.Keys)
            {
                if (sb.Length != 0)
                {
                    sb.Append('&');
                }
                UrlEncode(key, sb);
                sb.Append('=');
                UrlEncode(queryData.Get(key).UnsafeValue, sb);
            }
            return sb.ToString();
        }

        static void UrlEncode(string text, StringBuilder sb)
        {
            var bytes = new byte[8];
            var count = text.Length;
            for (var index = 0; index != count; ++index)
            {
                var ch = text[index];

                if (char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '~' || ch == '_')
                {
                    sb.Append(ch);
                }
                else if (ch == ' ')
                {
                    sb.Append('+');
                }
                else
                {
                    var byteCount = Encoding.UTF8.GetBytes(text, index, 1, bytes, 0);
                    for (var byteIndex = 0; byteIndex != byteCount; ++byteIndex)
                    {
                        sb.Append('%');
                        sb.Append("0123456789ABCDEF"[bytes[byteIndex] / 0x10]);
                        sb.Append("0123456789ABCDEF"[bytes[byteIndex] & 0x0f]);
                    }
                }
            }
        }

        static BodyDelegate RequestBody(string postBody, Encoding encoding)
        {
            return (write, flush, end, cancel) =>
            {
                try
                {
                    var data = new ArraySegment<byte>(encoding.GetBytes(postBody));
                    write(data);
                    end(null);
                }
                catch (Exception ex)
                {
                    end(ex);
                }
            };
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
