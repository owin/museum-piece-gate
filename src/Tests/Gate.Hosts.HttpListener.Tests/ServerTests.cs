using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Gate.Helpers;
using Gate.Middleware;
using Owin;
using NUnit.Framework;

namespace Gate.Hosts.HttpListener.Tests
{
    [TestFixture]
    public class ServerTests
    {
        [Test]
        public void ServerCanBeCreatedAndDisposed()
        {
            var server = ServerFactory.Create(call => { throw new NotImplementedException(); }, 8090, "");
            server.Dispose();
        }

        [Test]
        public void PathMayBeNullOrEmpty()
        {
            ServerFactory.Create(Wilson.App(), 8090, null).Dispose();
            ServerFactory.Create(Wilson.App(), 8090, "/").Dispose();
        }

        [Test]
        public void ServerWillRespondToRequests()
        {
            using (ServerFactory.Create(Wilson.App(), 8090, null))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:8090");
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var text = reader.ReadToEnd();
                        Assert.That(text, Is.StringContaining("hello world"));
                    }
                }
            }
        }

        [Test]
        public void ExceptionsWillReturnStatus500()
        {
            using (ServerFactory.Create(Wilson.App(), 8090, null))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:8090/?flip=crash");

                var exception = Assert.Throws<WebException>(() => request.GetResponse().Close());
                var response = (HttpWebResponse)exception.Response;
                Assert.That((int)response.StatusCode, Is.EqualTo(500));
            }
        }

        [Test]
        public void RequestsMayHavePostBody()
        {
            var requestData = new MemoryStream();
            AppDelegate app = call =>
            {
                call.Body.CopyTo(requestData);
                return Wilson.App().Invoke(call);
            };

            using (ServerFactory.Create(app, 8090, null))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:8090/");
                request.Method = "POST";
                using (var requestStream = request.GetRequestStream())
                {
                    var bytes = Encoding.Default.GetBytes("This is a test");
                    requestStream.Write(bytes, 0, bytes.Length);
                }

                request.GetResponse().Close();

                var requestText = Encoding.Default.GetString(requestData.ToArray());
                Assert.That(requestText, Is.EqualTo("This is a test"));
            }
        }

        [Test]
        public void ResponseMayHaveContentLength()
        {
            AppDelegate app = call =>
            {
                var response = new Response();
                response.Headers.SetHeader("Content-Length", "12");
                response.Write("Hello world.");
                return response.EndAsync();
            };
            using (ServerFactory.Create(app, 8090, null))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:8090/");

                string text;
                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            text = reader.ReadToEnd();
                        }
                    }
                }
                Assert.That(text, Is.EqualTo("Hello world."));
            }
        }
    }
}
