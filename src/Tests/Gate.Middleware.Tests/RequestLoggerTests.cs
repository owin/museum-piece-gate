using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Owin;
using System.IO;
using System.Threading.Tasks;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class RequestLoggerTests
    {
        [Test]
        public void NoLogger_PassThrough()
        {
            RequestLogger middleware = new RequestLogger(env =>
            {
                return TaskHelpers.Completed();
            });

            middleware.Invoke(new Request().Environment).Wait();
        }

        [Test]
        public void Logger_Logged()
        {
            RequestLogger middleware = new RequestLogger(env =>
            {
                return TaskHelpers.Completed();
            });

            StringWriter writer = new StringWriter();
            Request request = new Request();
            request.TraceOutput = writer;
            middleware.Invoke(request.Environment).Wait();
            Assert.That(writer.GetStringBuilder().ToString(), Is.Not.EqualTo(string.Empty));
        }
    }
}
