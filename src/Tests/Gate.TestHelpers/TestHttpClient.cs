using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Owin;
using Owin.Builder;

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
            public int ResponseStatus { get; set; }
            public IDictionary<string, string[]> ResponseHeaders { get; set; }
            public Func<Stream, Task> ResponseBody { get; set; }
            public IDictionary<string, object> ResponseProperties { get; set; }

            public Exception Exception { get; set; }
        }

        /// <summary>
        /// Create an HttpClient that can be used to test an app.
        /// </summary>
        /// <param name="configuration">Delegate called to build the app being tested</param>
        /// <returns></returns>
        public static TestHttpClient ForConfiguration(Action<IAppBuilder> configuration)
        {
            var builder = new AppBuilder();
            configuration(builder);
            return ForAppDelegate((AppDelegate)builder.Build(typeof(AppDelegate)));
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

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
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
                        {OwinConstants.RequestPath, "/" + request.RequestUri.GetComponents(UriComponents.Path, UriFormat.Unescaped)},
                        {OwinConstants.RequestQueryString, request.RequestUri.GetComponents(UriComponents.Query, UriFormat.UriEscaped)},
                        {"System.Net.Http.HttpRequestMessage", request},
                    }
                };

                Calls.Add(call);

                return GetRequestBody(request)
                    .Then(body => App(new CallParameters
                    {
                        Environment = call.Environment,
                        Headers = GetRequestHeaders(request),
                        Body = body
                    }))
                    .Then(result =>
                    {
                        call.ResponseStatus = result.Status;
                        call.ResponseHeaders = result.Headers;
                        call.ResponseBody = result.Body;
                        call.HttpResponseMessage = MakeResponseMessage(result.Status, result.Headers, result.Body, result.Properties);
                        call.HttpResponseMessage.RequestMessage = request;
                        return call.HttpResponseMessage;
                    });
            }

            static Task<Stream> GetRequestBody(HttpRequestMessage request)
            {
                return request.Content == null ? TaskHelpers.FromResult(default(Stream)) : request.Content.ReadAsStreamAsync();
            }

            static IDictionary<string, string[]> GetRequestHeaders(HttpRequestMessage request)
            {
                IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = request.Headers;
                if (request.Content != null)
                    headers = headers.Concat(request.Content.Headers);

                var requestHeaders = headers.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
                return requestHeaders;
            }

            static HttpResponseMessage MakeResponseMessage(int status, IDictionary<string, string[]> headers, Func<Stream, Task> body, IDictionary<string, object> properties)
            {
                var response = new HttpResponseMessage((HttpStatusCode)status);

                if (body != null)
                {
                    response.Content = new BodyDelegateHttpContent(body);
                }

                if (properties != null)
                {
                    object value;
                    if (properties.TryGetValue("owin.ReasonPhrase", out value))
                    {
                        response.ReasonPhrase = Convert.ToString(value);
                    }
                }

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (!response.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            if (response.Content == null)
                            {
                                response.Content = new ByteArrayContent(new byte[0]);
                            }
                            response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                return response;
            }

            class BodyDelegateHttpContent : HttpContent
            {
                readonly Func<Stream, Task> _body;

                public BodyDelegateHttpContent(Func<Stream, Task> body)
                {
                    _body = body;
                }

                protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
                {
                    return _body(stream);
                }

                protected override bool TryComputeLength(out long length)
                {
                    length = 0;
                    return false;
                }
            }
        }
    }
}
