using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Gate.Helpers.Tests
{
    using ResultDelegate = Action< // result
        string, // status
        IDictionary<string, string>, // headers
        Func< // body
            Func< // next
                ArraySegment<byte>, // data
                Action, // continuation
                bool>, // async                    
            Action<Exception>, // error
            Action, // complete
            Action>>; // cancel
    using BodyDelegate = Func< // body
        Func< // next
            ArraySegment<byte>, // data
            Action, // continuation
            bool>, // async                    
        Action<Exception>, // error
        Action, // complete
        Action>; //cancel

    [TestFixture]
    public class ResponseTests
    {
        string _status;
        IDictionary<string, string> _headers;
        BodyDelegate _body;

        [SetUp]
        public void Init()
        {
            _status = null;
            _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _body = null;
        }

        void Result(string status, IDictionary<string, string> headers, BodyDelegate body)
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
                (data, _) =>
                {
                    memory.Write(data.Array, data.Offset, data.Count);
                    return false;
                },
                ex => wait.Set(),
                () => wait.Set());
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
            response.Finish();
            Assert.That(_status, Is.EqualTo("200 Blah"));
            Assert.That(_headers["Content-Type"], Is.EqualTo("text/blah"));
        }

        [Test]
        public void Write_calls_will_spool_until_finish_is_called()
        {
            new Response(Result) {Status = "200 Yep"}
                .Write("this")
                .Write("is")
                .Write("a")
                .Write("test")
                .Finish();

            Assert.That(_status, Is.EqualTo("200 Yep"));
            var data = Encoding.UTF8.GetString(Consume());
            Assert.That(data, Is.EqualTo("thisisatest"));
        }
    }
}