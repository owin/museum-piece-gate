using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Owin;
using Owin.Builder;
using System.Collections.Generic;

namespace Gate.Middleware.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    public class ShowExceptionsTests
    {
        AppFunc Build(Action<IAppBuilder> b)
        {
            var builder = new AppBuilder();
            b(builder);
            return (AppFunc)builder.Build(typeof(AppFunc));
        }

        private string ReadBody(MemoryStream body)
        {
            MemoryStream buffer = (MemoryStream)body;
            body.Seek(0, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(buffer.ToArray());
        }

        [Test]
        public void Normal_request_should_pass_through_unchanged()
        {
            var stack = Build(b => b
                .UseShowExceptions()
                .UseFunc<AppFunc>(_ => appEnv =>
                {
                    Response appResult = new Response(appEnv) { StatusCode = 200 };
                    appResult.Headers.SetHeader("Content-Type", "text/plain");
                    appResult.Headers.SetHeader("Content-Length", "5");
                    appResult.Write("Hello");
                    return TaskHelpers.Completed();
                }));

            Request request = new Request();
            Response response = new Response(request.Environment);
            MemoryStream buffer = new MemoryStream();
            response.OutputStream = buffer;
            stack(request.Environment).Wait();

            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(ReadBody(buffer), Is.EqualTo("Hello"));
        }

        [Test]
        public void Request_fault_should_be_500_with_html_body()
        {
            var stack = Build(b => b
                .UseShowExceptions()
                .UseFunc<AppFunc>(_ => appEnv => { throw new ApplicationException("Kaboom"); }));

            Request request = new Request();
            Response response = new Response(request.Environment);
            MemoryStream buffer = new MemoryStream();
            response.OutputStream = buffer;
            stack(request.Environment).Wait();

            Assert.That(response.StatusCode, Is.EqualTo(500));
            Assert.That(response.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
            String bodyText = ReadBody(buffer);
            Assert.That(bodyText, Is.StringContaining("ApplicationException"));
            Assert.That(bodyText, Is.StringContaining("Kaboom"));
        }

        [Test]
        public void Sync_Exception_in_response_body_stream_should_be_formatted_as_it_passes_through()
        {
            var stack = Build(b => b
                .UseShowExceptions()
                .UseFunc<AppFunc>(_ => appEnv =>
                {
                    Response appResponse = new Response(appEnv);
                    appResponse.StatusCode = 200;
                    appResponse.Headers.SetHeader("Content-Type", "text/html");

                    byte[] bodyBytes = Encoding.ASCII.GetBytes("<p>so far so good</p>");
                    appResponse.OutputStream.Write(bodyBytes, 0, bodyBytes.Length);
                    throw new ApplicationException("failed sending body sync");
                }));

            Request request = new Request();
            Response response = new Response(request.Environment);
            MemoryStream buffer = new MemoryStream();
            response.OutputStream = buffer;
            stack(request.Environment).Wait();

            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
            String bodyText = ReadBody(buffer);
            Assert.That(bodyText, Is.StringContaining("<p>so far so good</p>"));
            Assert.That(bodyText, Is.StringContaining("failed sending body sync"));
        }

        [Test]
        public void Async_Exception_in_response_body_stream_should_be_formatted_as_it_passes_through()
        {
            var stack = Build(b => b
                .UseShowExceptions()
                .UseFunc<AppFunc>(_ => appEnv =>
                {
                    Response appResponse = new Response(appEnv);
                    appResponse.StatusCode = 200;
                    appResponse.Headers.SetHeader("Content-Type", "text/html");
                    
                    byte[] bodyBytes = Encoding.ASCII.GetBytes("<p>so far so good</p>");
                    appResponse.OutputStream.Write(bodyBytes, 0, bodyBytes.Length);
                    return TaskHelpers.FromError(new ApplicationException("failed sending body async"));
                }));

            Request request = new Request();
            Response response = new Response(request.Environment);
            MemoryStream buffer = new MemoryStream();
            response.OutputStream = buffer;
            stack(request.Environment).Wait();

            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
            String bodyText = ReadBody(buffer);
            Assert.That(bodyText, Is.StringContaining("<p>so far so good</p>"));
            Assert.That(bodyText, Is.StringContaining("failed sending body async"));
        }
    }
}