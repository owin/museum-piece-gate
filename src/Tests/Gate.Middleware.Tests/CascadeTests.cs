using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Owin;
using Owin.Builder;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class CascadeTests
    {
        AppDelegate Build(Action<IAppBuilder> b)
        {
            var builder = new AppBuilder();
            b(builder);
            return (AppDelegate)builder.Build(typeof(AppDelegate));
        }

        private Func<Stream, Task> CreateBody(String text)
        {
            return stream =>
            {
                byte[] body = Encoding.ASCII.GetBytes(text);
                stream.Write(body, 0, body.Length);
                return TaskHelpers.Completed();
            };
        }

        private String ReadBody(Func<Stream, Task> body)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                body(buffer).Wait();
                return Encoding.ASCII.GetString(buffer.ToArray());
            }
        }

        [Test]
        public void Cascade_with_no_apps_should_return_404()
        {
            var cascade = Build(b => b.RunCascade(Enumerable.Empty<AppDelegate>().ToArray()));

            ResultParameters result = cascade(new Request().Call).Result;

            Assert.That(result.Status, Is.EqualTo(404));
        }

        [Test]
        public void Cascade_with_app_calls_through()
        {
            var cascade = Build(b => b.RunCascade((AppDelegate)(call => new Response(200).EndAsync())));
            ResultParameters result = cascade(new Request().Call).Result;
            Assert.That(result.Status, Is.EqualTo(200));
        }

        [Test]
        public void Cascade_will_pass_along_to_first_non_404_app()
        {
            AppDelegate app1 = call => new Response(404).EndAsync();
            AppDelegate app2 = call => new Response(200).EndAsync();
            AppDelegate app3 = call => TaskHelpers.FromError<ResultParameters>(
                new ApplicationException("This should not have been invoked"));

            var cascade = Build(b => b.RunCascade(app1, app2, app3));
            ResultParameters result = cascade(new Request().Call).Result;
            Assert.That(result.Status, Is.EqualTo(200));
        }
        /*
        [Test]
        public void Cascade_works_when_result_is_not_on_same_thread()
        {
            var app1 = new FakeApp("404 Not Found", "") {SendAsync = true};
            var app2 = new FakeApp("200 OK", "Hello world") {SendAsync = true};
            app2.Headers.SetHeader("Content-Type", "text/plain");
            var app3 = new FakeApp("404 Not Found", "") {SendAsync = true};
            var cascade = Build(b => b.RunCascade(app1.AppDelegate, app2.AppDelegate, app3.AppDelegate));
            var host = new FakeHost(cascade);
            var response = host.GET("/");
            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.BodyText, Is.EqualTo("Hello world"));
            Assert.That(app1.AppDelegateInvoked, Is.True);
            Assert.That(app2.AppDelegateInvoked, Is.True);
            Assert.That(app3.AppDelegateInvoked, Is.False);
        }*/
    }
}