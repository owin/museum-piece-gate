using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Owin.Builder;
using Owin;
using System.IO;

namespace Gate.Middleware.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    public class TraceTests
    {
        private string Call(Action<IAppBuilder> pipe, Request request)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = (AppFunc)builder.Build(typeof(AppFunc));
            var env = request.Environment;
            var resp = new Response(env);
            MemoryStream buffer = new MemoryStream();
            resp.OutputStream = buffer;
            app(env).Wait();
            return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
        }

        [Test]
        public void Should_pass_through_GET()
        {
            Request initialRequest = Request.Create();
            initialRequest.Method = "GET";
            var responseBody = Call(b => b
                .UseTrace()
                .UseGate((request, response) => { response.Write("Hello World"); })
                , initialRequest);
            Assert.That(responseBody, Is.EqualTo("Hello World"));
        }

        [Test]
        public void Should_echo_TRACE()
        {
            Request request = Request.Create();
            request.Method = "TRACE";
            request.PathBase = "/Base/";
            request.Path = "Path";
            request.QueryString = "Query";
            request.Protocol = "HTTP/1.0";
            request.HostWithPort = "localhost:8080";
            request.Headers["Custom-Header"] = new string[] { "v1, v2", "v3" };

            var responseBody = Call(b => b
                .UseTrace()
                .UseGate((req, resp) => { resp.Write("Hello World"); throw new NotImplementedException(); })
                , request);

            Assert.That(responseBody.Contains("Hello World"), Is.False);
            Assert.That(responseBody, Is.EqualTo(
                "TRACE /Base/Path?Query HTTP/1.0\r\nHost: localhost:8080\r\nCustom-Header: v1, v2\r\nCustom-Header: v3\r\n"));
        }
    }
}
