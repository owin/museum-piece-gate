using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gate.Middleware.StaticFiles;
using Gate.Middleware.Utils;
using NUnit.Framework;
using Owin;

namespace Gate.Middleware.Tests.StaticFiles
{
    [TestFixture]
    public class FileServerTests
    {
        string root;
        FileServer fileServer;

        private ResultParameters GetFile(string path)
        {
            Request request = new Request();
            request.Path = path;
            return fileServer.Invoke(request.Call).Result;
        }

        private String ReadBody(Func<Stream, Task> body)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                body(buffer).Wait();
                return Encoding.ASCII.GetString(buffer.ToArray());
            }
        }

        [SetUp]
        public void Setup()
        {
            root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            fileServer = new FileServer(root);
        }

        [Test]
        public void FileServer_serves_files()
        {
            Assert.That(GetFile("/kayak.png").Status, Is.EqualTo(200));
        }

        [Test]
        public void FileServer_returns_404_on_missing_file()
        {
            Assert.That(GetFile("/scripts/horses.js").Status, Is.EqualTo(404));
        }

        [Test]
        public void FileServer_sets_LastModified_header()
        {
            var fileInfo = new FileInfo(Path.Combine(root, "kayak.png"));

            Assert.That(GetFile("/kayak.png").Headers.GetHeader("Last-Modified"), Is.EqualTo(fileInfo.LastWriteTimeUtc.ToHttpDateString()));
        }

        [Test]
        public void FileServer_does_not_decode_request_path()
        {
            // kayak.png, url encoded
            Assert.That(GetFile("/%6B%61%79%61%6B%2E%70%6E%67").Status, Is.EqualTo(404));
        }

        [Test]
        public void FileServer_returns_403_on_directory_traversal_attempt()
        {
            Assert.That(GetFile("/../ccinfo.txt").Status, Is.EqualTo(403));
        }

        [Test]
        public void FileServer_returns_correct_byte_range_in_body()
        {
            Request request = new Request();
            request.Path = "/test.txt";
            request.Headers.SetHeader("Range", "bytes=22-33");
            var result = fileServer.Invoke(request.Call).Result;

            Assert.That(result.Status, Is.EqualTo(206)); // Partial content
            Assert.That(result.Headers.GetHeader("Content-Length"), Is.EqualTo("12"));
            Assert.That(result.Headers.GetHeader("Content-Range"), Is.EqualTo("bytes 22-33/193"));
            Assert.That(ReadBody(result.Body), Is.EqualTo("-*- test -*-"));
        }

        [Test]
        public void FileServer_returns_error_for_unsatisfiable_byte_range()
        {
            Request request = new Request();
            request.Path = "/test.txt";
            request.Headers.SetHeader("Range", "bytes=1234-5678");
            var result = fileServer.Invoke(request.Call).Result;

            Assert.That(result.Status, Is.EqualTo(416)); //  Requested Range Not Satisfiable
            Assert.That(result.Headers.GetHeader("Content-Range"), Is.EqualTo("bytes */193"));
        }
    }
}
