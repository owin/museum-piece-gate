﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate;
using Gate.Hosts.Kayak;
using Gate.Owin;
using NUnit.Framework;
using Kayak.Http;
using Kayak;

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
        IDictionary<string, IEnumerable<string>> headers;
        BodyDelegate body;

        public Action OnRequest;
        public IDictionary<string, object> Env;

        public StaticApp(string status, IDictionary<string, IEnumerable<string>> headers, BodyDelegate body)
        {
            this.status = status;
            this.headers = headers;
            this.body = body;
        }

        public void Invoke(IDictionary<string, object> env, ResultDelegate response, Action<Exception> fault)
        {
            Env = env;
            if (OnRequest != null)
                OnRequest();
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
                    new Dictionary<string, IEnumerable<string>>(), 
                    (next, error, complete) => ((Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>) ((write, fault, end) =>
                    {
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("12345")), null);
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("67890")), null);
                        end();
                        return () => { };
                    }))(next, error, complete));

            var requestDelegate = new GateRequestDelegate(app.Invoke, new Dictionary<string, object>());

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(new Environment(app.Env).BodyAction, Is.Null);
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
                    new Dictionary<string, IEnumerable<string>>()
                    {
                        { "Transfer-Encoding", new[]{"chunked"} }
                    },
                    (next, error, complete) => ((Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>) ((write, fault, end) =>
                    {
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("12345")), null);
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("67890")), null);
                        end();
                        return () => { };
                    }))(next, error, complete));

            var requestDelegate = new GateRequestDelegate(app.Invoke, new Dictionary<string, object>());

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(new Environment(app.Env).BodyAction, Is.Null);
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
            
            var bodyAction = new Environment(app.Env).BodyAction; 
            Assert.That(bodyAction, Is.Not.Null);
            var body = ((BodyDelegate) ((next, error, complete) => bodyAction(next, error, complete))).Consume();
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

            var app = new StaticApp(null, null, null);

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
            var app = new StaticApp(null, null, null);

            IDictionary<string, object> appContext = null;

            app.OnRequest = () =>
            {
                appContext = app.Env;
            };

            var requestDelegate = new GateRequestDelegate(app.Invoke, null);
            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            var env = new Environment(appContext);

            Assert.That(env.BodyAction, Is.Null);
            Assert.That(env.Headers, Is.Not.Null);
            Assert.That(env.Method, Is.Not.Null);
            Assert.That(env.Path, Is.Not.Null);
            Assert.That(env.PathBase, Is.Not.Null);
            Assert.That(env.QueryString, Is.Not.Null);
            Assert.That(env.Scheme, Is.EqualTo("http"));
            Assert.That(env.Version, Is.EqualTo("1.0"));
        }
    }
}
