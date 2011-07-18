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
        public void Adds_content_length_header_if_none()
        {
            var requestDelegate = new GateRequestDelegate(App);

            requestDelegate.OnRequest(new HttpRequestHead() { }, null, mockResponseDelegate);

            Assert.That(mockResponseDelegate.Head.Headers.Keys, Contains.Item("Content-Length"));
            Assert.That(mockResponseDelegate.Head.Headers["Content-Length"], Is.EqualTo("10"));
        }

        public void App(IDictionary<string, object> env, ResultDelegate result, Action<Exception> error)
        {
            result("200 OK", new Dictionary<string, string>(), Delegates.ToDelegate((write, fault, end) =>
            {
                write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("12345")), null);
                write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("67890")), null);
                end();
                return () => { };
            }));
        }

    }
}
