using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate;
using Gate.Kayak;
using NUnit.Framework;
using Kayak;
using Kayak.Http;

namespace Gate.Kayak.Tests
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
            buffer.Add(data);
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

        public static BufferingConsumer Consume(this BodyDelegate del)
        {
            var c = new BufferingConsumer();
            del((data, ct) => c.OnData(data, ct), e => c.OnError(e), () => c.OnEnd());
            return c;
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
        string status;
        IDictionary<string, string> headers;
        BodyDelegate body;

        public IDictionary<string, object> Env;

        public StaticApp(string status, IDictionary<string, string> headers, BodyDelegate body)
        {
            this.status = status;
            this.headers = headers;
            this.body = body;
        }

        public void Invoke(IDictionary<string, object> env, ResultDelegate response, Action<Exception> fault)
        {
            Env = env;
            response(status, headers, body);
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
        public void Adds_content_length_response_header_if_none()
        {
            var app = new StaticApp(
                    "200 OK", 
                    new Dictionary<string, string>(), 
                    Delegates.ToDelegate((write, fault, end) =>
                    {
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("12345")), null);
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("67890")), null);
                        end();
                        return () => { };
                    }));

            var requestDelegate = new GateRequestDelegate(app.Invoke, new Dictionary<string, object>());

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(new Environment(app.Env).Body, Is.Null);
            Assert.That(mockResponseDelegate.Head.Headers.Keys, Contains.Item("Content-Length"));
            Assert.That(mockResponseDelegate.Head.Headers["Content-Length"], Is.EqualTo("10"));

            var body = mockResponseDelegate.Body.Consume();
            Assert.That(body.Buffer.GetString(), Is.EqualTo("1234567890"));
            Assert.That(body.GotEnd, Is.True);
        }

        [Test]
        public void Does_not_add_content_length_response_header_if_transfer_encoding_chunked()
        {
            var app = new StaticApp(
                    "200 OK",
                    new Dictionary<string, string>()
                    {
                        { "Transfer-Encoding", "chunked" }
                    },
                    Delegates.ToDelegate((write, fault, end) =>
                    {
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("12345")), null);
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("67890")), null);
                        end();
                        return () => { };
                    }));

            var requestDelegate = new GateRequestDelegate(app.Invoke, new Dictionary<string, object>());

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(new Environment(app.Env).Body, Is.Null);
            Assert.IsFalse(mockResponseDelegate.Head.Headers.Keys.Contains("Content-Length"), "should not contain Content-Length");
            // chunks are not chunked-encoded at this level. eventually kayak will do this automatically.
            var body = mockResponseDelegate.Body.Consume();
            Assert.That(body.Buffer.GetString(), Is.EqualTo("1234567890"));
            Assert.That(body.GotEnd, Is.True);
        }

        [Test]
        public void Request_body_is_passed_through()
        {
            var app = new StaticApp(null, null, null);

            var requestDelegate = new GateRequestDelegate(app.Invoke, new Dictionary<string, object>());

            requestDelegate.OnRequest(new HttpRequestHead() { }, new MockDataProducer(c =>
            {
                c.OnData(new ArraySegment<byte>(Encoding.ASCII.GetBytes("12345")), null);
                c.OnData(new ArraySegment<byte>(Encoding.ASCII.GetBytes("67890")), null);
                c.OnEnd();
                return () => { };
            }), mockResponseDelegate);
            
            var bodyAction = new Environment(app.Env).Body; 
            Assert.That(bodyAction, Is.Not.Null);
            var body = Delegates.ToDelegate(bodyAction).Consume();
            Assert.That(body.Buffer.GetString(), Is.EqualTo("1234567890"));
            Assert.That(body.GotEnd, Is.True);
        }
    }
}
