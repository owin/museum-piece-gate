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

        public StaticApp(string status, IDictionary<string, string> headers, BodyDelegate body)
        {
            this.status = status;
            this.headers = headers;
            this.body = body;
        }

        public void Invoke(IDictionary<string, object> env, ResultDelegate response, Action<Exception> fault)
        {
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
            var requestDelegate = new GateRequestDelegate(
                CreateApp(
                    "200 OK", 
                    new Dictionary<string, string>(), 
                    Delegates.ToDelegate((write, fault, end) =>
                    {
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("12345")), null);
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("67890")), null);
                        end();
                        return () => { };
                    })), null);

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(mockResponseDelegate.Head.Headers.Keys, Contains.Item("Content-Length"));
            Assert.That(mockResponseDelegate.Head.Headers["Content-Length"], Is.EqualTo("10"));
            Assert.That(mockResponseDelegate.Body.Consume().Buffer.GetString(), Is.EqualTo("1234567890"));
        }

        [Test]
        public void Does_not_add_content_length_response_header_if_transfer_encoding_chunked()
        {
            var requestDelegate = new GateRequestDelegate(
                CreateApp(
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
                    })), null);

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.IsFalse(mockResponseDelegate.Head.Headers.Keys.Contains("Content-Length"), "should not contain Content-Length");
            // chunks are not chunked-encoded at this level. eventually kayak will do this automatically.
            Assert.That(mockResponseDelegate.Body.Consume().Buffer.GetString(), Is.EqualTo("1234567890"));
        }

        public AppDelegate CreateApp(string status, IDictionary<string, string> headers, BodyDelegate body)
        {
            return new StaticApp(status, headers, body).Invoke;
        }
    }
}
