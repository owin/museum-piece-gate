using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Owin;
using System.IO;
using System.Threading.Tasks;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class PassiveValidatorTests
    {
        private static string ReadResponseBody(ResultParameters result)
        {
            if (result.Body != null)
            {
                Stream buffer = new MemoryStream();
                result.Body(buffer).Wait();
                buffer.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new StreamReader(buffer);
                return reader.ReadToEnd();
            }
            return string.Empty;
        }

        [Test]
        public void NormalPassThrough_Success()
        {
            AppDelegate middleware = PassiveValidator.Middleware(
                call =>
                {
                    Response response = new Response(200);
                    return response.EndAsync();
                });

            Request request = new Request();
            request.Completed = new TaskCompletionSource<object>().Task;
            request.Method = "GET";
            request.Path = "/foo";
            request.PathBase = string.Empty;
            request.Protocol = "HTTP/1.1";
            request.QueryString = "foo=bar";
            request.Scheme = "http";
            request.Version = "1.0";
            request.HostWithPort = "hostname:8080";

            ResultParameters result = middleware(request.Call).Result;

            Assert.That(ReadResponseBody(result), Is.EqualTo(string.Empty));
            Assert.That(result.Status, Is.EqualTo(200));
        }
    }
}
