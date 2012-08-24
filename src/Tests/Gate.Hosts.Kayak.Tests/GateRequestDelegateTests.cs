using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kayak;
using Kayak.Http;
using NUnit.Framework;

namespace Gate.Hosts.Kayak.Tests
{
    class Buffer
    {
        List<ArraySegment<byte>> buffer;

        public Buffer()
        {
            buffer = new List<ArraySegment<byte>>();
        }
        public void Add(ArraySegment<byte> data)
        {
            var copy = new byte[data.Count];
            Array.Copy(data.Array, data.Offset, copy, 0, data.Count);
            buffer.Add(new ArraySegment<byte>(copy));
        }

        public string GetString()
        {
            return buffer.Aggregate("", (r, n) => r + Encoding.UTF8.GetString(n.Array, n.Offset, n.Count));
        }
    }

    static class DataConsumerExtensions
    {
        public static BufferingConsumer Consume(this IDataProducer producer)
        {
            var c = new BufferingConsumer();
            producer.Connect(c);
            return c;
        }
        /*
        public static BufferingConsumer Consume(this BodyDelegate del)
        {
            var c = new BufferingConsumer();
            del(
                c.OnData,
                ex =>
                {
                    if (ex == null) c.OnEnd();
                    else c.OnError(ex);
                },
                CancellationToken.None);
            return c;
        }
        */
        public static BufferingConsumer Consume(this Stream producer)
        {
            throw new NotImplementedException();
        }
    }

    class BufferingConsumer : IDataConsumer
    {
        public Exception Exception;
        public Buffer Buffer;
        public bool GotEnd;

        public BufferingConsumer()
        {
            Buffer = new Buffer();
        }

        public bool OnData(ArraySegment<byte> data, Action continuation)
        {
            Buffer.Add(data);
            return false;
        }

        public void OnEnd()
        {
            if (GotEnd)
                throw new Exception("Already got OnEnd");

            GotEnd = true;
        }

        public void OnError(Exception e)
        {
            Exception = e;
        }
    }

    class MockDataProducer : IDataProducer
    {
        Func<IDataConsumer, Action> subscribe;

        public MockDataProducer(Func<IDataConsumer, Action> subscribe)
        {
            this.subscribe = subscribe;
        }

        public IDisposable Connect(IDataConsumer channel)
        {
            return new Disposable(subscribe(channel));
        }
    }

    class MockResponseDelegate : IHttpResponseDelegate
    {
        public HttpResponseHead Head;
        public IDataProducer Body;

        public void OnResponse(HttpResponseHead head, IDataProducer body)
        {
            Head = head;
            Body = body;
        }
    }

    public class StaticApp
    {
        Response response;

        public Action OnRequest;
        public IDictionary<string, object> Env;

        public StaticApp(Response response)
        {
            this.response = response;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            Env = env;
            if (OnRequest != null)
                OnRequest();

            return response.EndAsync();
        }
    }

    [TestFixture]
    public class GateRequestDelegateTests
    {
        MockResponseDelegate mockResponseDelegate;

        [SetUp]
        public void SetUp()
        {
            mockResponseDelegate = new MockResponseDelegate();
        }

        [Test]
        [Ignore] // TODO: Implement the response body stream
        public void Adds_connection_close_response_header_if_no_length_or_encoding()
        {
            Request request = new Request();
            Response expectedResponse = new Response(request.Environment);
            expectedResponse.OutputStream = new MemoryStream();
            expectedResponse.Write("12345");
            expectedResponse.Write("67890");
            expectedResponse.OutputStream.Seek(0, SeekOrigin.Begin);
            var app = new StaticApp(expectedResponse);

            var requestDelegate = new GateRequestDelegate(app.Invoke, new Dictionary<string, object>());

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(app.Env.Get<Stream>(OwinConstants.RequestBody), Is.EqualTo(Stream.Null));
            Assert.That(mockResponseDelegate.Head.Headers.Keys, Contains.Item("Connection"));
            Assert.That(mockResponseDelegate.Head.Headers["Connection"], Is.EqualTo("close"));

            Assert.That(mockResponseDelegate.Body, Is.Not.Null);
            var body = mockResponseDelegate.Body.Consume();
            Assert.That(body.Buffer.GetString(), Is.EqualTo("1234567890"));
            Assert.That(body.GotEnd, Is.True);
        }

        [Test]
        [Ignore] // TODO: Implement the response body stream
        public void Does_not_add_content_length_response_header_if_transfer_encoding_chunked()
        {
            Request request = new Request();
            Response expectedResponse = new Response(request.Environment);
            expectedResponse.OutputStream = new MemoryStream();
            expectedResponse.Headers.SetHeader("Transfer-Encoding", "chunked");
            expectedResponse.Write("12345");
            expectedResponse.Write("67890");
            expectedResponse.OutputStream.Seek(0, SeekOrigin.Begin);
            var app = new StaticApp(expectedResponse);

            var requestDelegate = new GateRequestDelegate(app.Invoke, new Dictionary<string, object>());

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(app.Env.Get<Stream>(OwinConstants.RequestBody), Is.EqualTo(Stream.Null));
            Assert.IsFalse(mockResponseDelegate.Head.Headers.Keys.Contains("Content-Length"), "should not contain Content-Length");
            // chunks are not chunked-encoded at this level. eventually kayak will do this automatically.
            Assert.That(mockResponseDelegate.Body, Is.Not.Null);
            var body = mockResponseDelegate.Body.Consume();
            Assert.That(body.Buffer.GetString(), Is.EqualTo("1234567890"));
            Assert.That(body.GotEnd, Is.True);
        }

        [Test]
        [Ignore] // TODO: Implement the request body stream
        public void Request_body_is_passed_through()
        {
            Request request = new Request();
            Response expectedResponse = new Response(request.Environment);
            var app = new StaticApp(expectedResponse);

            var requestDelegate = new GateRequestDelegate(app.Invoke, new Dictionary<string, object>());

            requestDelegate.OnRequest(new HttpRequestHead() { }, new MockDataProducer(c =>
            {
                c.OnData(new ArraySegment<byte>(Encoding.ASCII.GetBytes("12345")), null);
                c.OnData(new ArraySegment<byte>(Encoding.ASCII.GetBytes("67890")), null);
                c.OnEnd();
                return () => { };
            }), mockResponseDelegate);

            var bodyStream = app.Env.Get<Stream>(OwinConstants.RequestBody);
            Assert.That(bodyStream, Is.Not.Null);
            var body = bodyStream.Consume();
            Assert.That(body.Buffer.GetString(), Is.EqualTo("1234567890"));
            Assert.That(body.GotEnd, Is.True);
        }

        [Test]
        public void Context_passed_to_constructor_is_passed_through_and_not_modified_by_requests()
        {
            var context = new Dictionary<string, object>() 
            {
                { "Key", "Value" }
            };

            Request request = new Request();
            Response expectedResponse = new Response(request.Environment);
            var app = new StaticApp(expectedResponse);

            IDictionary<string, object> appContext = null;

            app.OnRequest = () =>
            {
                app.Env["OtherKey"] = "OtherValue";
                appContext = app.Env;
            };

            var requestDelegate = new GateRequestDelegate(app.Invoke, context);
            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(context.ContainsKey("Key"), Is.True);
            Assert.That(context["Key"], Is.EqualTo("Value"));
            Assert.That(context.ContainsKey("OtherKey"), Is.False);

            Assert.That(appContext.ContainsKey("Key"), Is.True);
            Assert.That(appContext["Key"], Is.EqualTo("Value"));
            Assert.That(appContext.ContainsKey("OtherKey"), Is.True);
            Assert.That(appContext["OtherKey"], Is.EqualTo("OtherValue"));
        }

        [Test]
        public void Environment_items_conform_to_spec_by_default()
        {
            Request request = new Request();
            Response expectedResponse = new Response(request.Environment);
            var app = new StaticApp(expectedResponse);

            IDictionary<string, object> env = null;

            app.OnRequest = () =>
            {
                env = app.Env;
            };

            var requestDelegate = new GateRequestDelegate(app.Invoke, null);
            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            var req = new Request(env);

            Assert.That(req.Body, Is.EqualTo(Stream.Null));
            Assert.That(req.Headers, Is.Not.Null);
            Assert.That(req.Method, Is.Not.Null);
            Assert.That(req.Path, Is.Not.Null);
            Assert.That(req.PathBase, Is.Not.Null);
            Assert.That(req.QueryString, Is.Not.Null);
            Assert.That(req.Scheme, Is.EqualTo("http"));
            Assert.That(req.Version, Is.EqualTo("1.0"));
        }
    }
}
