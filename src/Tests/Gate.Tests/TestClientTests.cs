using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Gate.TestHelpers;
using NUnit.Framework;
using Owin;
using System.Threading.Tasks;
using System.IO;

namespace Gate.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    public class TestClientTests
    {
        AppFunc NotFound = call => { call.Set("owin.ResponseStatusCode", 404); return TaskHelpers.Completed(); };

        [Test]
        public void ForConfigurationShouldCallWithBuilderAndReturnHttpClient()
        {
            var called = 0;
            var httpClient = TestHttpClient.ForConfiguration(builder => { ++called; });
            Assert.That(called, Is.EqualTo(1));
            Assert.That(httpClient, Is.Not.Null);
        }

        [Test]
        public void RequestPassesThroughToApplication()
        {
            var client = TestHttpClient.ForAppDelegate(NotFound);

            var result = client.GetAsync("http://localhost/foo?hello=world").Result;

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(client.Calls.Count, Is.EqualTo(1));
            Assert.That(client.Calls.Single().ResponseStatus, Is.EqualTo(404));
        }

        [Test]
        public void ResponseBodyIsReturned()
        {
            var client = TestHttpClient.ForAppDelegate(ReturnText("Hello world"));
            var result = client.GetStringAsync("http://localhost/foo?hello=world").Result;
            Assert.That(result, Is.EqualTo("Hello world"));
        }

        [Test]
        public void ResponseHeadersAreReturned()
        {
            var client = TestHttpClient.ForAppDelegate(ReturnText("Hello world"));
            client.GetStringAsync("http://localhost/foo?hello=world").Wait();

            var call = client.Calls.Single();

            Assert.IsNotNull(call, "Single");
            Assert.IsNotNull(call.ResponseHeaders, "ResponseHeaders");
            Assert.IsNotNull(call.ResponseHeaders["Content-Type"].Single(), "Content-Type");

            Assert.That(call.ResponseHeaders["Content-Type"].Single(), Is.EqualTo("text/plain"));

            Assert.IsNotNull(call.HttpResponseMessage, "Response");
            Assert.IsNotNull(call.HttpResponseMessage.Content, "Response.Content");
            Assert.IsNotNull(call.HttpResponseMessage.Content.Headers.ContentType, "ContentType");

            Assert.That(call.HttpResponseMessage.Content.Headers.ContentType.MediaType, Is.EqualTo("text/plain"));

            Assert.That(call.ResponseHeaders["X-Server"].Single(), Is.EqualTo("inproc"));
            Assert.That(call.HttpResponseMessage.Headers.GetValues("X-Server").Single(), Is.EqualTo("inproc"));
        }

        [Test]
        public void RequestIsAssociatedWithResponse()
        {
            var client = TestHttpClient.ForAppDelegate(ReturnText("Hello world"));
            var responseMessage = client.GetAsync("http://localhost/foo?hello=world").Result;

            Assert.That(responseMessage.RequestMessage, Is.Not.Null);
            Assert.That(responseMessage.RequestMessage.RequestUri.Query, Is.EqualTo("?hello=world"));
        }

        [Test]
        public void RequestContentShouldBeSent()
        {
            var client = new TestHttpClient(EchoRequestBody());
            var requestContent = new StringContent("Hello world", Encoding.UTF8, "text/plain");
            var responseMessage = client.PostAsync("http://localhost/", requestContent).Result;
            var result = responseMessage.Content.ReadAsStringAsync().Result;
            Assert.That(result, Is.EqualTo("Hello world"));
        }

        AppDelegate EchoRequestBody()
        {
            return call =>
            {
                ResultParameters result = new ResultParameters();
                result.Status = 200;
                result.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    {"Content-Type", call.Headers["Content-Type"].ToArray()}
                };
                
                MemoryStream buffer = new MemoryStream();
                call.Body.CopyTo(buffer);
                buffer.Seek(0, SeekOrigin.Begin);
                 
                result.Body = stream =>
                {
                    buffer.CopyTo(stream);                    
                    return TaskHelpers.Completed();
                };

                return TaskHelpers.FromResult(result);
            };
        }

        AppDelegate ReturnText(string text)
        {
            return call =>
            {
                ResultParameters result = new ResultParameters();
                result.Status = 200;
                result.Properties = new Dictionary<string, object>();
                result.Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    {"Content-Type", new[] {"text/plain"}},
                    {"X-Server", new[] {"inproc"}}
                };

                result.Body = stream =>
                {
                    byte[] body = Encoding.ASCII.GetBytes(text);
                    stream.Write(body, 0, body.Length);
                    return TaskHelpers.Completed();
                };

                return TaskHelpers.FromResult(result);
            };
        }
    }
}