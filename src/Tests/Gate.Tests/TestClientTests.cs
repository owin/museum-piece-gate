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

namespace Gate.Tests
{
    [TestFixture]
    public class TestClientTests
    {
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
            var client = TestHttpClient.ForAppDelegate(NotFound.Call);

            var result = client.GetAsync("http://localhost/foo?hello=world").Result;

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(client.Calls.Count, Is.EqualTo(1));
            Assert.That(client.Calls.Single().ResponseStatus, Is.EqualTo("404 Not Found"));
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
            Assert.That(client.Calls.Single().ResponseHeaders["Content-Type"].Single(), Is.EqualTo("text/plain"));
            Assert.That(client.Calls.Single().HttpResponseMessage.Content.Headers.ContentType.MediaType, Is.EqualTo("text/plain"));

            Assert.That(client.Calls.Single().ResponseHeaders["X-Server"].Single(), Is.EqualTo("inproc"));
            Assert.That(client.Calls.Single().HttpResponseMessage.Headers.GetValues("X-Server").Single(), Is.EqualTo("inproc"));
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
            return (env, result, fault) =>
            {
                var callDisposed = (CancellationToken)env["host.CallDisposed"];
                var requestHeaders = (IDictionary<string, IEnumerable<string>>)env["owin.RequestHeaders"];
                var requestBody = (BodyDelegate)env["owin.RequestBody"];

                var responseHeaders = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    {"Content-Type", requestHeaders["Content-Type"]}
                };
                BodyDelegate responseBody = (write, flush, end, cancel) => end(null);

                requestBody.Invoke(
                    data =>
                    {
                        var copy = new byte[data.Count];
                        Array.Copy(data.Array, data.Offset, copy, 0, data.Count);
                        var next = responseBody;
                        responseBody = (write, flush, end, cancel) =>
                        {
                            write(new ArraySegment<byte>(copy));
                            next(write, flush, end, cancel);
                        };
                        return false;
                    },
                    flushed => false,
                    ex =>
                    {
                        if (ex != null)
                        {
                            fault(ex);
                        }
                        else
                        {
                            result.Invoke(
                                "200 OK",
                                responseHeaders,
                                responseBody);
                        }
                    },
                    callDisposed);
            };
        }

        AppDelegate ReturnText(string text)
        {
            return (env, result, fault) => result(
                "200 OK",
                new Dictionary<string, IEnumerable<string>>
                {
                    {"Content-Type", new[] {"text/plain"}},
                    {"X-Server", new[] {"inproc"}}
                },
                (write, flush, end, cancel) =>
                {
                    write(new ArraySegment<byte>(Encoding.ASCII.GetBytes(text)));
                    end(null);
                });
        }
    }
}
