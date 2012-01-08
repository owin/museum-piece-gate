using System;
using System.Linq;
using System.Text;
using Gate.Builder;
using Gate.TestHelpers;
using NUnit.Framework;
using Gate.Owin;
using Gate.Middleware;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ShowExceptionsTests
    {
        AppDelegate Build(Action<IAppBuilder> b)
        {
            return AppBuilder.BuildConfiguration(b);
        }

        [Test]
        public void Normal_request_should_pass_through_unchanged()
        {
            var app = new FakeApp("200 OK", "Hello");
            app.Headers.SetHeader("Content-Type", "text/plain");

            var stack = Build(b => b
                .UseShowExceptions()
                .Run(app.AppDelegate));

            var host = new FakeHost(stack);

            var response = host.GET("/");

            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.BodyText, Is.EqualTo("Hello"));
        }

        [Test]
        public void Request_fault_should_be_500_with_html_body()
        {
            var app = new FakeApp(new ApplicationException("Kaboom"));

            var stack = Build(b => b
                .UseShowExceptions()
                .Run(app.AppDelegate));

            var host = new FakeHost(stack);

            var response = host.GET("/");

            Assert.That(response.Status, Is.EqualTo("500 Internal Server Error"));
            Assert.That(response.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("ApplicationException"));
            Assert.That(response.BodyText, Is.StringContaining("Kaboom"));
        }

        [Test]
        public void Exception_in_response_body_stream_should_be_formatted_as_it_passes_through()
        {
            var app = new FakeApp {Status = "200 OK"};
            app.Headers.SetHeader("Content-Type", "text/html");
            app.Body = (next, error, complete) =>
            {
                next(new ArraySegment<byte>(Encoding.UTF8.GetBytes("<p>so far so good</p>")), null);
                error(new ApplicationException("failed sending body"));
                return () => { };
            };

            var stack = Build(b => b
                .UseShowExceptions()
                .Run(app.AppDelegate));

            var host = new FakeHost(stack);

            var response = host.GET("/");

            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
            Assert.That(response.BodyText, Is.StringContaining("<p>so far so good</p>"));
            Assert.That(response.BodyText, Is.StringContaining("failed sending body"));
        }

        [Test, Ignore("Not sure why this has compiler error, but didn't before")]
        public void Stack_frame_should_parse_with_and_without_line_numbers()
        {
            throw new Exception("Not sure why this has compiler error, but didn't before");

            //    var frames = ShowExceptionsExtensions.StackFrames(new[]{"  at foo in bar:line 42\r\n"}).ToArray();
            //    Assert.That(frames.Length, Is.EqualTo(1));
            //    Assert.That(frames[0].Function, Is.EqualTo("foo"));
            //    Assert.That(frames[0].File, Is.EqualTo("bar"));
            //    Assert.That(frames[0].Line, Is.EqualTo(42));

            //    frames = ShowExceptionsExtensions.StackFrames(new[]{"  at foo\r\n"}).ToArray();
            //    Assert.That(frames.Length, Is.EqualTo(1));
            //    Assert.That(frames[0].Function, Is.EqualTo("foo"));
            //    Assert.That(frames[0].File, Is.EqualTo(""));
            //    Assert.That(frames[0].Line, Is.EqualTo(0));
        }
    }
}