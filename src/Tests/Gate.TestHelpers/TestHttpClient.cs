using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using Gate.Builder;
using Owin;

namespace Gate.TestHelpers
{
    public class TestHttpClient : HttpClient
    {
        readonly AppDelegateMessageHandler _handler;

        public TestHttpClient(AppDelegate app)
            : this(new AppDelegateMessageHandler(app))
        {
        }

        TestHttpClient(AppDelegateMessageHandler handler)
            : base(handler)
        {
            _handler = handler;
        }

        public AppDelegate App { get { return _handler.App; } }
        public IList<Call> Calls { get { return _handler.Calls; } }

        public class Call
        {
            public HttpRequestMessage HttpRequestMessage { get; set; }
            public HttpResponseMessage HttpResponseMessage { get; set; }

            public IDictionary<string, object> Environment { get; set; }
            public string ResponseStatus { get; set; }
            public IDictionary<string, IEnumerable<string>> ResponseHeaders { get; set; }
            public BodyDelegate ResponseBody { get; set; }

            public Exception Exception { get; set; }
        }

        /// <summary>
        /// Create an HttpClient that can be used to test an app.
        /// </summary>
        /// <param name="configuration">Delegate called to build the app being tested</param>
        /// <returns></returns>
        public static TestHttpClient ForConfiguration(Action<IAppBuilder> configuration)
        {
            return ForAppDelegate(AppBuilder.BuildConfiguration(configuration));
        }

        /// <summary>
        /// Create an HttpClient that can be used to test an app.
        /// </summary>
        /// <param name="app">Delegate that will be called by the HttpClient</param>
        /// <returns></returns>
        public static TestHttpClient ForAppDelegate(AppDelegate app)
        {
            return new TestHttpClient(app);
        }

        class AppDelegateMessageHandler : HttpMessageHandler
        {
            public AppDelegateMessageHandler(AppDelegate app)
            {
                App = app;
                Calls = new List<Call>();
            }

            public IList<Call> Calls { get; private set; }
            public AppDelegate App { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var call = new Call
                {
                    HttpRequestMessage = request,
                    Environment = new Dictionary<string, object>
                    {
                        {OwinConstants.Version, "1.0"},
                        {OwinConstants.RequestMethod, request.Method.ToString()},
                        {OwinConstants.RequestScheme, request.RequestUri.Scheme},
                        {OwinConstants.RequestPathBase, ""},
                        {OwinConstants.RequestPath, request.RequestUri.GetComponents(UriComponents.Path, UriFormat.Unescaped)},
                        {OwinConstants.RequestQueryString, request.RequestUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped)},
                        {OwinConstants.RequestHeaders, RequestHeaders(request)},
                        {OwinConstants.RequestBody, MakeRequestBody(request)},
                        {"host.CallDisposed", cancellationToken},
                        {"System.Net.Http.HttpRequestMessage", request},
                    }
                };

                Calls.Add(call);

                var tcs = new TaskCompletionSource<HttpResponseMessage>();
                App(
                    call.Environment,
                    (status, headers, body) =>
                    {
                        call.ResponseStatus = status;
                        call.ResponseHeaders = headers;
                        call.ResponseBody = body;

                        var response = MakeResponseMessage(status, headers, body);
                        response.RequestMessage = request;
                        call.HttpResponseMessage = response;
                        tcs.TrySetResult(response);
                    },
                    ex =>
                    {
                        call.Exception = ex;
                        tcs.TrySetException(ex);
                    });
                return tcs.Task;
            }

            static IDictionary<string, IEnumerable<string>> RequestHeaders(HttpRequestMessage request)
            {
                IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = request.Headers;
                if (request.Content != null)
                    headers = headers.Concat(request.Content.Headers);

                var requestHeaders = headers.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                return requestHeaders;
            }

            static BodyDelegate MakeRequestBody(HttpRequestMessage request)
            {
                if (request.Content == null)
                {
                    return (write, flush, end, cancel) => end(null);
                }

                return (write, flush, end, cancel) =>
                {
                    var task = request.Content.CopyToAsync(new BodyDelegateStream(write, flush, cancel));
                    if (task.IsFaulted)
                    {
                        end(task.Exception);
                    }
                    else if (task.IsCompleted)
                    {
                        end(null);
                    }
                    else
                    {
                        task.ContinueWith(t => { end(t.IsFaulted ? t.Exception : null); }, cancel);
                    }
                };
            }


            static HttpResponseMessage MakeResponseMessage(string status, IDictionary<string, IEnumerable<string>> headers, BodyDelegate body)
            {
                var httpStatusCode = (HttpStatusCode)int.Parse(status.Substring(0, 3));
                var response = new HttpResponseMessage(httpStatusCode);
                //response.
                if (body != null)
                {
                    response.Content = MakeResponseContent(body);
                }

                foreach (var header in headers)
                {
                    if (!response.Headers.TryAddWithoutValidation(header.Key, header.Value))
                    {
                        if (response.Content == null)
                        {
                            response.Content = MakeResponseContent((write, flush, end, cancel) => end(null));
                        }
                        response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                return response;
            }

            static HttpContent MakeResponseContent(BodyDelegate body)
            {
                return new BodyDelegateHttpContent(body);
            }

            class BodyDelegateHttpContent : HttpContent
            {
                readonly BodyDelegate _body;

                public BodyDelegateHttpContent(BodyDelegate body)
                {
                    _body = body;
                }

                protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
                {
                    var tcs = new TaskCompletionSource<object>();
                    _body.Invoke(
                        data =>
                        {
                            stream.Write(data.Array, data.Offset, data.Count);
                            return false;
                        },
                        flushed =>
                        {
                            stream.Flush();
                            return false;
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
                        CancellationToken.None);
                    return tcs.Task;
                }

                protected override bool TryComputeLength(out long length)
                {
                    length = 0;
                    return false;
                }
            }

            class BodyDelegateStream : Stream
            {
                readonly Func<ArraySegment<byte>, bool> _write;
                readonly Func<Action, bool> _flush;
                readonly CancellationToken _cancellationToken;

                public BodyDelegateStream(Func<ArraySegment<byte>, bool> write, Func<Action, bool> flush, CancellationToken cancellationToken)
                {
                    _write = write;
                    _flush = flush;
                    _cancellationToken = cancellationToken;
                }

                public override void Flush()
                {
                    _flush(null);
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    throw new NotImplementedException();
                }

                public override void SetLength(long value)
                {
                    throw new NotImplementedException();
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    throw new NotImplementedException();
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    _write(new ArraySegment<byte>(buffer, offset, count));
                }

                public override bool CanRead
                {
                    get { return false; }
                }

                public override bool CanSeek
                {
                    get { return false; }
                }

                public override bool CanWrite
                {
                    get { return true; }
                }

                public override long Length
                {
                    get { throw new NotImplementedException(); }
                }

                public override long Position
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }
            }

        }
    }
}
