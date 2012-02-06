using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Owin;
using NUnit.Framework;

namespace Gate.Tests
{
    [TestFixture]
    public class ResponseTests
    {
        string _status;
        IDictionary<string, IEnumerable<string>> _headers;
        BodyDelegate _body;

        [SetUp]
        public void Init()
        {
            _status = null;
            _headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
            _body = null;
        }

        void Result(string status, IDictionary<string, IEnumerable<string>> headers, BodyDelegate body)
        {
            _status = status;
            _headers = headers;
            _body = body;
        }

        byte[] Consume()
        {
            var memory = new MemoryStream();
            var wait = new ManualResetEvent(false);
            _body(
                data =>
                {
                    memory.Write(data.Array, data.Offset, data.Count);
                    return false;
                },
                _ => false,
                ex => wait.Set(),
                CancellationToken.None);
            wait.WaitOne();
            return memory.ToArray();
        }

        [Test]
        public void Finish_will_call_result_delegate_with_current_status_and_headers()
        {
            var response = new Response(Result)
            {
                Status = "200 Blah",
                ContentType = "text/blah",
            };

            Assert.That(_status, Is.Null);
            response.End();
            Assert.That(_status, Is.EqualTo("200 Blah"));
            Assert.That(_headers.GetHeader("Content-Type"), Is.EqualTo("text/blah"));
        }

        [Test]
        public void Write_calls_will_spool_until_finish_is_called()
        {
            new Response(Result) { Status = "200 Yep" }
                .Write("this")
                .Write("is")
                .Write("a")
                .Write("test")
                .End();

            Assert.That(_status, Is.EqualTo("200 Yep"));
            var data = Encoding.UTF8.GetString(Consume());
            Assert.That(data, Is.EqualTo("thisisatest"));
        }
    }
}