using System;
using NUnit.Framework;
using Owin;
using Owin.Builder;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace Gate.Middleware.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    public class ContentLengthTests
    {
        private IDictionary<string, string[]> CallPipe(Action<IAppBuilder> pipe)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = (AppFunc)builder.Build(typeof(AppFunc));
            Request request = new Request();
            Response response = new Response(request.Environment);
            response.OutputStream = new MemoryStream();
            app(request.Environment).Wait();
            return response.Headers;
        }

        [Test]
        public void Content_length_is_added_if_body_is_zero_length()
        {
            var result = CallPipe(b => b
                .UseContentLength()
                .UseDirect((request, response) => response.EndAsync()));

            Assert.That(result.GetHeader("content-length"), Is.EqualTo("0"));
        }

        [Test]
        public void Content_length_is_added()
        {
            var result = CallPipe(b => b
                .UseContentLength()
                .UseDirect(
                    (request, response) => 
                    {
                        response.Write("hello ");
                        response.Write("world.");
                        return response.EndAsync();
                    }));

            Assert.That(result.GetHeader("content-length"), Is.EqualTo("12"));
        }

        [Test]
        public void Content_length_is_not_changed()
        {
            var result = CallPipe(b => b
                .UseContentLength()
                .UseDirect(
                    (request, response) => 
                    {
                        response.Headers.SetHeader("content-length", "69");
                        return response.EndAsync();
                    }));

            Assert.That(result.GetHeader("content-length"), Is.EqualTo("69"));
        }

        [Test]
        public void Content_length_is_not_added_if_transfer_encoding_is_present()
        {
            var result = CallPipe(b => b
                .UseContentLength()
                .UseDirect(
                    (request, response) => 
                    {
                        response.Headers.SetHeader("transfer-encoding", "chunked");
                        return response.EndAsync();
                    }));

            Assert.That(result.ContainsKey("content-length"), Is.False);
        }

        [Test]
        [Sequential]
        public void Content_length_is_not_added_if_response_status_should_not_have_a_response_body(
            [Values(
            "204 No Content",
            "205 Reset Content",
            "304 Not Modified",
            "100 Continue",
            "101 Switching Protocols",
            "112 Whatever"
            // and all other 1xx statuses...
            )] string status)
        {
            var result = CallPipe(b => b
                .UseContentLength()
                .UseDirect(
                    (request, response) => 
                    {
                        response.Status = status;
                        return response.EndAsync();
                    }));

            Assert.That(result.ContainsKey("content-length"), Is.False);
        }
    }
}

