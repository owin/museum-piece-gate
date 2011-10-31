using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Threading;
using System.Threading.Tasks;
using Gate.Builder;
using Gate.Helpers;

namespace Gate.Wcf
{
    [ServiceContract]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class GateWcfService
    {
        readonly AppDelegate _app;

        GateWcfService(AppDelegate app)
        {
            _app = app;
        }

        public static WebServiceHost Create(Uri baseUri)
        {
            return Create(baseUri, ConfigurationManager.AppSettings["Gate.Startup"]);
        }

        public static WebServiceHost Create(Uri baseUri, string configurationString)
        {
            var app = AppBuilder.BuildConfiguration(configurationString);
            return Create(baseUri, app);
        }

        public static WebServiceHost Create(Uri baseUri, AppDelegate app)
        {
            var host = new WebServiceHost(new GateWcfService(app), baseUri);
            host.AddServiceEndpoint(typeof (GateWcfService), new WebHttpBinding(), "");
            host.Open();
            return host;
        }

        [WebInvoke(UriTemplate = "*", Method = "*")]
        [OperationContract(AsyncPattern = true)]
        public IAsyncResult BeginHandleRequests(Stream requestBody, AsyncCallback callback, object asyncState)
        {
            var webContext = WebOperationContext.Current;
            var env = CreateOwinEnvironment(webContext, requestBody);

            var tcs = new TaskCompletionSource<Message>(asyncState);
            if (callback != null)
                tcs.Task.ContinueWith(t => callback(t), TaskContinuationOptions.ExecuteSynchronously);

            _app(
                env,
                (status, headers, body) => tcs.SetResult(CreateOwinResponse(webContext, status, headers, body)),
                tcs.SetException);

            return tcs.Task;
        }

        public Message EndHandleRequests(IAsyncResult asyncResult)
        {
            var task = (Task<Message>) asyncResult;
            return task.Result;
        }

        static IDictionary<string, object> CreateOwinEnvironment(WebOperationContext webRequest, Stream requestBody)
        {
            var incomingRequest = webRequest.IncomingRequest;
            var baseUri = incomingRequest.UriTemplateMatch.BaseUri;
            var requestUri = incomingRequest.UriTemplateMatch.RequestUri;

            var baseUriPathUnescaped = "/" + baseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            var requestUriPathUnescaped = "/" + requestUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);

            var pathBase = baseUriPathUnescaped.TrimEnd('/');
            var path = requestUriPathUnescaped.Substring(pathBase.Length);
            var queryString = requestUri.Query.TrimStart('?');

            var headers = incomingRequest.Headers.AllKeys
                .ToDictionary(key => key, incomingRequest.Headers.Get);

            var env = new Dictionary<string, object>();

            new Environment(env)
            {
                Version = "1.0",
                Method = incomingRequest.Method,
                Scheme = requestUri.Scheme,
                PathBase = pathBase,
                Path = path,
                QueryString = queryString,
                Headers = headers,
                Body = Body.FromStream(requestBody),
            };
            return env;
        }

        static Message CreateOwinResponse(
            WebOperationContext webResponse,
            string status,
            IDictionary<string, string> headers,
            BodyDelegate body)
        {
            //TODO: hardenning

            var statusCode = int.Parse(status.Substring(0, 3));
            webResponse.OutgoingResponse.StatusCode = (HttpStatusCode) statusCode;
            webResponse.OutgoingResponse.StatusDescription = status.Substring(4);

            foreach (var header in Split(headers))
            {
                webResponse.OutgoingResponse.Headers.Add(header.Key, header.Value);
            }

            string contentType;
            if (!headers.TryGetValue("Content-Type", out contentType))
                contentType = "text/plain";

            return webResponse.CreateStreamResponse(
                stream =>
                {
                    var done = new ManualResetEvent(false);
                    body(
                        (data, _) =>
                        {
                            stream.Write(data.Array, data.Offset, data.Count);
                            return false;
                        },
                        ex => done.Set(),
                        () => done.Set()
                        );
                    done.WaitOne();
                },
                contentType);
        }

        static IEnumerable<KeyValuePair<string, string>> Split(IEnumerable<KeyValuePair<string, string>> headers)
        {
            return headers.SelectMany(kv => kv.Value.Split("\r\n".ToArray(), StringSplitOptions.RemoveEmptyEntries).Select(v => new KeyValuePair<string, string>(kv.Key, v)));
        }
    }
}