using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Gate.Helpers;
using NUnit.Framework;

namespace Gate.Hosts.Manos.Tests
{
    [TestFixture]
    public class ServerTests
    {
        [Test]
        public void ServerCanBeCreatedAndDisposed()
        {
            var server = Server.Create((env, result, fault) => { throw new NotImplementedException(); }, 8090, "");
            server.Dispose();
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
    }
}
