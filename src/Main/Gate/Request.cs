using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gate.Utils;

namespace Gate
{
    using BodyAction = Action<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, CancellationToken>;

    public class Request : Environment
    {
        static readonly char[] CommaSemicolon = new[] { ',', ';' };

        public Request(IDictionary<string, object> env)
            : base(env)
        {
        }

        public IDictionary<string, string> Query
        {
            get
            {
                var text = QueryString;
                if (Get<string>("Gate.Request.Query#text") != text ||
                    Get<IDictionary<string, string>>("Gate.Request.Query") == null)
                {
                    this["Gate.Request.Query#text"] = text;
                    this["Gate.Request.Query"] = ParamDictionary.Parse(text);
                }
                return Get<IDictionary<string, string>>("Gate.Request.Query");
            }
        }

        static readonly char[] CookieParamSeparators = new[] { ';', ',' };
        public IDictionary<string, string> Cookies
        {
            get
            {
                var cookies = Get<IDictionary<string, string>>("Gate.Request.Cookies#dictionary");
                if (cookies == null)
                {
                    cookies = new Dictionary<string, string>(StringComparer.Ordinal);
                    Env["Gate.Request.Cookies#dictionary"] = cookies;
                }

                var text = Headers.GetHeader("Cookie");
                if (Get<string>("Gate.Request.Cookies#text") != text)
                {
                    cookies.Clear();
                    foreach (var kv in ParamDictionary.ParseToEnumerable(text, CookieParamSeparators))
                    {
                        if (!cookies.ContainsKey(kv.Key))
                            cookies.Add(kv);
                    }
                    Env["Gate.Request.Cookies#text"] = text;
                }
                return cookies;
            }
        }

        public bool HasFormData
        {
            get
            {
                var mediaType = MediaType;
                return (Method == "POST" && string.IsNullOrEmpty(mediaType))
                    || mediaType == "application/x-www-form-urlencoded"
                        || mediaType == "multipart/form-data";
            }
        }

        public bool HasParseableData
        {
            get
            {
                var mediaType = MediaType;
                return mediaType == "application/x-www-form-urlencoded"
                    || mediaType == "multipart/form-data";
            }
        }


        public string ContentType
        {
            get
            {
                return Headers.GetHeader("Content-Type");
            }
        }

        public string MediaType
        {
            get
            {
                var contentType = ContentType;
                if (contentType == null)
                    return null;
                var delimiterPos = contentType.IndexOfAny(CommaSemicolon);
                return delimiterPos < 0 ? contentType : contentType.Substring(0, delimiterPos);
            }
        }

        public Stream OpenInputStream()
        {
            var inputStream = new RequestStream();
            inputStream.Start(BodyDelegate, CallDisposed);
            return inputStream;
        }

        public Task CopyToStreamAsync(Stream stream, CancellationToken cancel)
        {
            var tcs = new TaskCompletionSource<object>();
            BodyDelegate.Invoke(
                (data, callback) =>
                {
                    try
                    {
                        if (data.Array == null)
                        {
                            stream.Flush();
                            return false;
                        }
                        if (callback == null)
                        {
                            stream.Write(data.Array, data.Offset, data.Count);
                            return false;
                        }
                        var sr = stream.BeginWrite(
                            data.Array,
                            data.Offset,
                            data.Count,
                            ar =>
                            {
                                if (ar.CompletedSynchronously)
                                {
                                    return;
                                }
                                try
                                {
                                    stream.EndWrite(ar);
                                }
                                catch (Exception ex)
                                {
                                    tcs.TrySetException(ex);
                                }
                                finally
                                {
                                    try
                                    {
                                        callback();
                                    }
                                    catch (Exception ex)
                                    {
                                        tcs.TrySetException(ex);
                                    }
                                }
                            },
                            null);

                        if (!sr.CompletedSynchronously)
                        {
                            return true;
                        }

                        stream.EndWrite(sr);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                        return false;
                    }
                },
                ex =>
                {
                    if (ex == null)
                    {
                        tcs.TrySetResult(null);
                    }
                    else
                    {
                        tcs.TrySetException(ex);
                    }
                },
                cancel);
            return tcs.Task;
        }

        public Task<string> ReadTextAsync()
        {
            var text = Get<string>("Gate.Request.Text");
            var thisInput = BodyDelegate;
            var lastInput = Get<object>("Gate.Request.Text#input");
            var tcs = new TaskCompletionSource<string>();
            if (text != null && ReferenceEquals(thisInput, lastInput))
            {
                tcs.SetResult(text);
                return tcs.Task;
            }
            var encoding = Encoding.UTF8;

            var sb = new StringBuilder();

            thisInput.Invoke(
                (data, callback) => false,
                ex =>
                {
                    if (ex != null)
                    {
                        tcs.SetException(ex);
                    }
                    else
                    {
                        tcs.SetResult(sb.ToString());
                    }
                },
                CallDisposed);
            return tcs.Task;
        }

        public string ReadText()
        {
            return ReadTextAsync().Result;
        }

        public Task<IDictionary<string, string>> ReadFormAsync()
        {
            if (!HasFormData && !HasParseableData)
            {
                var tcs = new TaskCompletionSource<IDictionary<string, string>>();
                tcs.SetResult(ParamDictionary.Parse(""));
                return tcs.Task;
            }

            var form = Get<IDictionary<string, string>>("Gate.Request.Form");
            var thisInput = Get<object>(OwinConstants.RequestBody);
            var lastInput = Get<object>("Gate.Request.Form#input");
            if (form != null && ReferenceEquals(thisInput, lastInput))
            {
                var tcs = new TaskCompletionSource<IDictionary<string, string>>();
                tcs.SetResult(form);
                return tcs.Task;
            }

            return ReadTextAsync().ContinueWith(t =>
            {
                form = ParamDictionary.Parse(t.Result);
                this["Gate.Request.Form#input"] = thisInput;
                this["Gate.Request.Form"] = form;
                return form;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public IDictionary<string, string> ReadForm()
        {
            return ReadFormAsync().Result;
        }


        public string HostWithPort
        {
            get
            {
                var hostHeader = Headers.GetHeader("Host");
                if (!string.IsNullOrWhiteSpace(hostHeader))
                {
                    return hostHeader;
                }

                var serverName = Get<string>("server.SERVER_NAME");
                if (string.IsNullOrWhiteSpace(serverName))
                    serverName = Get<string>("server.SERVER_ADDRESS");
                var serverPort = Get<string>("server.SERVER_PORT");

                return serverName + ":" + serverPort;
            }
        }

        public string Host
        {
            get
            {
                var hostHeader = Headers.GetHeader("Host");
                if (!string.IsNullOrWhiteSpace(hostHeader))
                {
                    var delimiter = hostHeader.IndexOf(':');
                    return delimiter < 0 ? hostHeader : hostHeader.Substring(0, delimiter);
                }
                var serverName = Get<string>("server.SERVER_NAME");
                if (string.IsNullOrWhiteSpace(serverName))
                    serverName = Get<string>("server.SERVER_ADDRESS");
                return serverName;
            }
        }
    }
}