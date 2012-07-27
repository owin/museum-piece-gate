using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gate.Builder;
using NUnit.Framework;
using Owin;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ShowExceptionsTests
    {
        AppDelegate Build(Action<IAppBuilder> b)
        {
            return AppBuilder.BuildPipeline<AppDelegate>(b);
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
        public void Normal_request_should_pass_through_unchanged()
        {
            var stack = Build(b => b
                .UseShowExceptions()
                .Run(appCall =>
                {
                    Response appResult = new Response(200);
                    appResult.Headers.SetHeader("Content-Type", "text/plain");
                    appResult.Headers.SetHeader("Content-Length", "5");
                    appResult.Write("Hello");
                    return appResult.EndAsync();
                }));

            ResultParameters result = stack(new Request().Call).Result;

            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(ReadBody(result.Body), Is.EqualTo("Hello"));
        }

        [Test]
        public void Request_fault_should_be_500_with_html_body()
        {
            var stack = Build(b => b
                .UseShowExceptions()
                .Run(appCall => { throw new ApplicationException("Kaboom"); }));

            ResultParameters result = stack(new Request().Call).Result;

            Assert.That(result.Status, Is.EqualTo(500));
            Assert.That(result.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
            String bodyText = ReadBody(result.Body);
            Assert.That(bodyText, Is.StringContaining("ApplicationException"));
            Assert.That(bodyText, Is.StringContaining("Kaboom"));
        }

        [Test]
        public void Sync_Exception_in_response_body_stream_should_be_formatted_as_it_passes_through()
        {
            var stack = Build(b => b
                .UseShowExceptions()
                .Run(appCall =>
                {
                    ResultParameters rawResult = new ResultParameters();
                    rawResult.Body = stream =>
                        {
                            byte[] bodyBytes = Encoding.ASCII.GetBytes("<p>so far so good</p>");
                            stream.Write(bodyBytes, 0, bodyBytes.Length);
                            throw new ApplicationException("failed sending body sync");
                        };
                    rawResult.Status = 200;
                    Response appResult = new Response(rawResult);
                    appResult.Headers.SetHeader("Content-Type", "text/html");
                    appResult.Start();
                    return appResult.ResultTask;
                }));

            ResultParameters result = stack(new Request().Call).Result;

            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(result.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
            String bodyText = ReadBody(result.Body);
            Assert.That(bodyText, Is.StringContaining("<p>so far so good</p>"));
            Assert.That(bodyText, Is.StringContaining("failed sending body sync"));
        }

        [Test]
        public void Async_Exception_in_response_body_stream_should_be_formatted_as_it_passes_through()
        {
            var stack = Build(b => b
                .UseShowExceptions()
                .Run(appCall =>
                {
                    Response appResult = new Response(200);
                    appResult.Headers.SetHeader("Content-Type", "text/html");
                    appResult.StartAsync().Then(
                        resp1 =>
                        {
                            resp1.Write("<p>so far so good</p>");
                            resp1.Error(new ApplicationException("failed sending body async"));
                        });
                    return appResult.ResultTask;
                }));

            ResultParameters result = stack(new Request().Call).Result;

            Assert.That(result.Status, Is.EqualTo(200));
            Assert.That(result.Headers.GetHeader("Content-Type"), Is.EqualTo("text/html"));
            String bodyText = ReadBody(result.Body);
            Assert.That(bodyText, Is.StringContaining("<p>so far so good</p>"));
            Assert.That(bodyText, Is.StringContaining("failed sending body async"));
        }
    }
}