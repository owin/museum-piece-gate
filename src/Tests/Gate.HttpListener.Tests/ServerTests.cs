using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Gate.Helpers;
using NUnit.Framework;

namespace Gate.HttpListener.Tests
{
    [TestFixture]
    public class ServerTests
    {
        [Test]
        public void ServerCanBeCreatedAndDisposed()
        {
            var server = Server.Create(null, 8090, "");
            server.Dispose();
        }

        [Test]
        public void PathMayBeNullOrEmpty()
        {
            Server.Create(Wilson.App(), 8090, null).Dispose();
            Server.Create(Wilson.App(), 8090, "/").Dispose();
        }

        [Test]
        public void ServerWillRespondToRequests()
        {
            using (Server.Create(Wilson.App(), 8090))
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
            using (Server.Create(Wilson.App(), 8090))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:8090/?flip=crash");

                var exception = Assert.Throws<WebException>(() => request.GetResponse().Close());
                var response = (HttpWebResponse)exception.Response;
                Assert.That((int)response.StatusCode, Is.EqualTo(500));
            }
        }
    }
}
