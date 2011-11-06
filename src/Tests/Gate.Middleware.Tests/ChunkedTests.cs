using System;
using NUnit.Framework;
using Gate.Owin;
using Gate.Builder;
using Gate.TestHelpers;
using System.Text;
using System.Collections.Generic;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ChunkedTests
    {
        [Test]
        public void Does_not_encode_if_content_length_present()
        {
            var response = AppUtils.Call(AppBuilder.BuildConfiguration(b => b
                .Chunked()
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                    { { "Content-Length", "12" }, { "Content-Type", "text/plain" } },
                    (onNext, onError, onComplete) => {
                        onNext(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello ")), null);
                        onNext(new ArraySegment<byte>(Encoding.ASCII.GetBytes("world.")), null);
                        onComplete();
                        return null;
                    }))));

            Assert.That(response.Headers.ContainsKey("transfer-encoding"), Is.False);
            Assert.That(response.BodyText, Is.EqualTo("hello world."));
        }
        
        [Test]
        public void Does_not_endcode_if_transfer_encoding_is_not_chunked()
        {
            var response = AppUtils.Call(AppBuilder.BuildConfiguration(b => b
                .Chunked()
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                    { { "transfer-encoding", "girl" }, { "Content-Type", "text/plain" } },
                    (onNext, onError, onComplete) => {
                        onNext(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello ")), null);
                        onNext(new ArraySegment<byte>(Encoding.ASCII.GetBytes("world.")), null);
                        onComplete();
                        return null;
                    }))));

            Assert.That(response.Headers["Transfer-Encoding"], Is.EqualTo("girl"));
            Assert.That(response.BodyText, Is.EqualTo("hello world."));
        }
        
        [Test]
        public void Encodes_if_content_length_is_not_present()
        {
            var response = AppUtils.Call(AppBuilder.BuildConfiguration(b => b
                .Chunked()
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                    { { "Content-Type", "text/plain" } },
                    (onNext, onError, onComplete) => {
                        onNext(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello ")), null);
                        onNext(new ArraySegment<byte>(Encoding.ASCII.GetBytes("world.")), null);
                        onComplete();
                        return null;
                    }))));

            Assert.That(response.BodyText, Is.EqualTo("6\r\nhello \r\n6\r\nworld.\r\n0\r\n\r\n"));
            Assert.That(response.Headers["Transfer-Encoding"], Is.EqualTo("chunked"));
        }
    }
}
