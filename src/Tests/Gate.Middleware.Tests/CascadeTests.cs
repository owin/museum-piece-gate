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
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    public class CascadeTests
    {
        AppFunc Build(Action<IAppBuilder> b)
        {
            var builder = new AppBuilder();
            b(builder);
            return (AppFunc)builder.Build(typeof(AppFunc));
        }

        [Test]
        public void Cascade_with_no_apps_should_return_404()
        {
            var cascade = Build(b => b.UseCascade(new AppFunc[0]));

            Request request = new Request();
            Response response = new Response(request.Environment);
            cascade(request.Environment).Wait();

            Assert.That(response.StatusCode, Is.EqualTo(404));
        }

        AppFunc SetStatusApp(int statusCode)
        {
            return env =>
            {
                var resp = new Response(env) {StatusCode = statusCode};
                return TaskHelpers.Completed();
            };
        }

        [Test]
        public void Cascade_with_app_calls_through()
        {
            var cascade = Build(b => b.UseCascade(SetStatusApp(200)));

            Request request = new Request();
            Response response = new Response(request.Environment);
            cascade(request.Environment).Wait();

            Assert.That(response.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void Cascade_will_pass_along_to_first_non_404_app()
        {
            AppFunc app1 = SetStatusApp(404);
            AppFunc app2 = SetStatusApp(200);
            AppFunc app3 = env => TaskHelpers.FromError<object>(
                new ApplicationException("This should not have been invoked"));

            var cascade = Build(b => b.UseCascade(app1, app2, app3));

            Request request = new Request();
            Response response = new Response(request.Environment);
            cascade(request.Environment).Wait();

            Assert.That(response.StatusCode, Is.EqualTo(200));
        }
        /*
        [Test]
        public void Cascade_works_when_result_is_not_on_same_thread()
        {
            var app1 = new FakeApp("404 Not Found", "") {SendAsync = true};
            var app2 = new FakeApp("200 OK", "Hello world") {SendAsync = true};
            app2.Headers.SetHeader("Content-Type", "text/plain");
            var app3 = new FakeApp("404 Not Found", "") {SendAsync = true};
            var cascade = Build(b => b.UseCascade(app1.AppFunc, app2.AppFunc, app3.AppFunc));
            var host = new FakeHost(cascade);
            var response = host.GET("/");
            Assert.That(response.Status, Is.EqualTo("200 OK"));
            Assert.That(response.BodyText, Is.EqualTo("Hello world"));
            Assert.That(app1.AppFuncInvoked, Is.True);
            Assert.That(app2.AppFuncInvoked, Is.True);
            Assert.That(app3.AppFuncInvoked, Is.False);
        }*/
    }
}