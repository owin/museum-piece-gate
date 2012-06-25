using System;
using System.Collections.Generic;
using Gate.Builder;
using Gate.TestHelpers;
using NUnit.Framework;
using System.Text;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ChunkedTests
    {
        [Test]
        public void ChunkPrefixHasCorrectResults()
        {
            AssertChunkPrefix(Chunked.ChunkPrefix(1), "1\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(15), "f\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x10), "10\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x80), "80\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0xff), "ff\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x10), "10\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x100), "100\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x1000), "1000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x10000), "10000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x100000), "100000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x1000000), "1000000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0x10000000), "10000000\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0), "0\r\n");
            AssertChunkPrefix(Chunked.ChunkPrefix(0xffffffff), "ffffffff\r\n");
        }

        static void AssertChunkPrefix(ArraySegment<byte> data, string expected)
        {
            Assert.That(Encoding.ASCII.GetString(data.Array, data.Offset, data.Count), Is.EqualTo(expected));
        }

        [Test]
        public void Does_not_encode_if_content_length_present()
        {
            var app = AppUtils.Simple(
                "200 OK",
                h => h
                    .SetHeader("Content-Length", "12")
                    .SetHeader("Content-Type", "text/plain"),
                write =>
                {
                    write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello ")));
                    write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("world.")));
                });

            var response = AppUtils.Call(AppBuilder.BuildPipeline(b => b
                .UseChunked()
                .Run(app)));

            Assert.That(response.Headers.ContainsKey("transfer-encoding"), Is.False);
            Assert.That(response.BodyText, Is.EqualTo("hello world."));
        }

        [Test]
        public void Does_not_encode_if_transfer_encoding_is_present()
        {
            var app = AppUtils.Simple(
                "200 OK",
                h => h
                    .SetHeader("transfer-encoding", "girl")
                    .SetHeader("Content-Type", "text/plain"),
                write =>
                {
                    write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello ")));
                    write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("world.")));
                });

            var response = AppUtils.Call(AppBuilder.BuildPipeline(b => b
                .UseChunked()
                .Run(app)));

            Assert.That(response.Headers.GetHeader("Transfer-Encoding"), Is.EqualTo("girl"));
            Assert.That(response.BodyText, Is.EqualTo("hello world."));
        }

        [Test]
        public void Encodes_if_content_length_is_not_present()
        {
            var app = AppUtils.Simple(
                "200 OK",
                h => h
                    .SetHeader("Content-Type", "text/plain"),
                write =>
                {
                    write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("hello ")));
                    write(new ArraySegment<byte>(Encoding.ASCII.GetBytes("world.")));
                });

            var response = AppUtils.Call(AppBuilder.BuildPipeline(b => b
                .UseChunked()
                .Run(app)));

            Assert.That(response.BodyText, Is.EqualTo("6\r\nhello \r\n6\r\nworld.\r\n0\r\n\r\n"));
            Assert.That(response.Headers.GetHeader("Transfer-Encoding"), Is.EqualTo("chunked"));
        }
    }
}
