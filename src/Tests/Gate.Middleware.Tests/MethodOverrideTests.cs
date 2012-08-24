using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Owin;
using System.IO;
using Owin.Builder;
using System.Collections.Generic;

namespace Gate.Middleware.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [TestFixture]
    class MethodOverrideTests
    {
        private void Call(Action<IAppBuilder> pipe, Request request)
        {
            var builder = new AppBuilder();
            pipe(builder);
            var app = (AppFunc)builder.Build(typeof(AppFunc));
            app(request.Environment).Wait();
        }

        private string ReadBody(Stream body)
        {
            MemoryStream buffer = (MemoryStream)body;
            body.Seek(0, SeekOrigin.Begin);
            return Encoding.ASCII.GetString(buffer.ToArray());
        }

        [Test]
        public void Method_is_overridden_if_override_present()
        {
            Request request = new Request();
            request.Method = "POST";
            request.Headers.SetHeader("x-http-method-override", "DELETE");
            Response response = new Response(request.Environment);
            response.OutputStream = new MemoryStream();

            Call(b => b
                .UseMethodOverride()
                .UseDirect(
                    (appRequest, appResponse) => 
                    {
                        appResponse.Write(appRequest.Method);
                        return appResponse.EndAsync();
                    }),
                request);

            Assert.That(ReadBody(response.OutputStream), Is.EqualTo("DELETE"));
        }

        [Test]
        public void Method_is_unchanged_if_override_not_present()
        {
            Request request = new Request();
            request.Method = "POST";
            Response response = new Response(request.Environment);
            response.OutputStream = new MemoryStream();

            Call(b => b
                .UseMethodOverride()
                .UseDirect(
                    (appRequest, appResponse) =>
                    {
                        appResponse.Write(appRequest.Method);
                        return appResponse.EndAsync();
                    }),
                request);

            Assert.That(ReadBody(response.OutputStream), Is.EqualTo("POST"));
        }
    }
}
