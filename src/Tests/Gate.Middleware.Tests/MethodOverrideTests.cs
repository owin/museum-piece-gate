using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gate.TestHelpers;
using Gate.Builder;
using Owin;
using System.IO;
using System.Threading;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    class MethodOverrideTests
    {
        private ResultParameters Call(Action<IAppBuilder> pipe, Request request)
        {
            AppDelegate app = AppBuilder.BuildPipeline<AppDelegate>(pipe);
            return app(request.Call).Result;
        }

        private string ReadBody(BodyDelegate body)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                body(buffer, CancellationToken.None).Wait();
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
                        response.Body.Write(appRequest.Method);
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
                        appResponse.Body.Write(appRequest.Method);
                        return appResponse.EndAsync();
                    }),
                request);

            Assert.That(ReadBody(result.Body), Is.EqualTo("POST"));
        }
    }
}
