using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Owin;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class RequestTracerTests
    {
        [Test]
        public void NoTracer_PassThrough()
        {
            RequestTracer middleware = new RequestTracer(env =>
            {
                return TaskHelpers.Completed();
            });

            middleware.Invoke(Request.Create().Environment).Wait();
        }

        [Test]
        public void Tracer_Traced()
        {
            RequestTracer middleware = new RequestTracer(env =>
            {
                return TaskHelpers.Completed();
            });

            StringWriter writer = new StringWriter();
            TextWriterTraceListener textListener = new TextWriterTraceListener(writer, "TestTracer");
            TraceSource traceSource = new TraceSource("TestSource", SourceLevels.All);
            traceSource.Listeners.Add(textListener);

            Request request = Request.Create();
            request.Environment["host.TraceSource"] = traceSource;
            middleware.Invoke(request.Environment).Wait();
            Assert.That(writer.GetStringBuilder().ToString(), Is.Not.EqualTo(string.Empty));
        }
    }
}
