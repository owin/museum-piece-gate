using System;
using Gate.Middleware;
using Gate.TestHelpers;
using Gate.Owin;
using Gate.Builder;
using NUnit.Framework;
using System.Text;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ContentLengthTests
    {        
        [Test]
        public void Content_length_is_added_if_body_is_zero_length()
        {
            var result = AppUtils.CallPipe(b => b
                .UseContentLength()
                .Simple(
                    "200 OK",
                    headers => { },
                    write => { }));

            Assert.That(result.Headers.GetHeader("content-length"), Is.EqualTo("0"));
        }

        [Test]
        public void Content_length_is_added()
        {
            var result = AppUtils.CallPipe(b => b
                .UseContentLength()
                .Simple(
                    "200 OK",
                    headers => { },
                    write =>
                    {
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello ")));
                        write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("world.")));
                    }));

            Assert.That(result.Headers.GetHeader("content-length"), Is.EqualTo("12"));
        }

        [Test]
        public void Content_length_is_not_changed()
        {
            var result = AppUtils.CallPipe(b => b
                .UseContentLength()
                .Simple(
                    "200 OK",
                    headers => headers.SetHeader("content-length", "69"),
                    write => { }));

            Assert.That(result.Headers.GetHeader("content-length"), Is.EqualTo("69"));
        }

        [Test]
        public void Content_length_is_not_added_if_transfer_encoding_is_present()
        {
            var result = AppUtils.CallPipe(b => b
                .UseContentLength()
                .Simple(
                    "200 OK",
                    headers => headers.SetHeader("transfer-encoding", "chunked"),
                    write => { }));

            Assert.That(result.Headers.ContainsKey("content-length"), Is.False);
        }

        [Test]
        [Sequential]
        public void Content_length_is_not_added_if_response_status_should_not_have_a_response_body(
            [Values(
            "204 No Content",
            "205 Reset Content",
            "304 Not Modified",
            "100 Continue",
            "101 Switching Protocols",
            "112 Whatever"
            // and all other 1xx statuses...
            )] string status)
        {
            var result = AppUtils.CallPipe(b => b
                .UseContentLength()
                .Simple(status, headers => { }, write => { }));

            Assert.That(result.Headers.ContainsKey("content-length"), Is.False);
        }
    }
}

