using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Owin;
using System.IO;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class RequestLoggerTests
    {
        [Test]
        public void NoLogger_PassThrough()
        {
            RequestLogger middleware = new RequestLogger(call =>
            {
                return new Response().EndAsync();
            });

            ResultParameters result = middleware.Invoke(new Request().Call).Result;
        }

        [Test]
        public void Logger_Logged()
        {
            RequestLogger middleware = new RequestLogger(call =>
            {
                return new Response().EndAsync();
            });

            StringWriter writer = new StringWriter();
            Request request = new Request();
            request.TraceOutput = writer;
            ResultParameters result = middleware.Invoke(request.Call).Result;
            Assert.That(writer.GetStringBuilder().ToString(), Is.Not.EqualTo(string.Empty));
        }
    }
}
