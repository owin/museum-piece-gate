using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Owin;
using System.IO;
using Owin.Builder;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    class MethodOverrideTests
    {
        private ResultParameters Call(Action<IAppBuilder> pipe, Request request)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = (AppDelegate)builder.Build(typeof(AppDelegate));
            return app(request.Call).Result;
        }

        private string ReadBody(Func<Stream, Task> body)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                body(buffer).Wait();
                return Encoding.ASCII.GetString(buffer.ToArray());
            }
        }

        [Test]
        public void Method_is_overridden_if_override_present()
        {
            Request request = new Request();
            request.Method = "POST";
            request.Headers.SetHeader("x-http-method-override", "DELETE");

            var result = Call(b => b
                .UseMethodOverride()
                .UseDirect(
                    (appRequest, response) => 
                    {
                        response.Write(appRequest.Method);
                        return response.EndAsync();
                    }),
                request);

            Assert.That(ReadBody(result.Body), Is.EqualTo("DELETE"));
        }

        [Test]
        public void Method_is_unchanged_if_override_not_present()
        {
            Request request = new Request();
            request.Method = "POST";

            var result = Call(b => b
                .UseMethodOverride()
                .UseDirect(
                    (appRequest, appResponse) =>
                    {
                        appResponse.Write(appRequest.Method);
                        return appResponse.EndAsync();
                    }),
                request);

            Assert.That(ReadBody(result.Body), Is.EqualTo("POST"));
        }
    }
}
