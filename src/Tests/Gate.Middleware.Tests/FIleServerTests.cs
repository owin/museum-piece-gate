using System;
using System.IO;
using Gate.Middleware.StaticFiles;
using Gate.Middleware.Utils;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class FileServerTests
    {
        string root;
        FileServer fileServer;

        [SetUp]
        public void Setup()
        {
            root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public");
            fileServer = new FileServer(root);
        }

        [Test]
        public void FileServer_serves_files()
        {
            var result = AppUtils.Call(fileServer.Invoke, "/kayak.png");

            Assert.That(result.Status, Is.EqualTo("200 OK"));
        }

        [Test]
        public void FileServer_returns_404_on_missing_file()
        {
            var result = AppUtils.Call(fileServer.Invoke, "/scripts/horses.js");

            Assert.That(result.Status, Is.EqualTo("404 Not Found"));
        }

        [Test]
        public void FileServer_sets_LastModified_header()
        {
            var fileInfo = new FileInfo(Path.Combine(root, "kayak.png"));

            var result = AppUtils.Call(fileServer.Invoke, "/kayak.png");

            Assert.That(result.Headers["Last-Modified"], Is.EqualTo(fileInfo.LastWriteTimeUtc.ToHttpDateString()));
        }

        [Test]
        public void FileServer_serves_file_with_url_encoded_filename()
        {
            var result = AppUtils.Call(fileServer.Invoke, "/%6B%61%79%61%6B%2E%70%6E%67");

            Assert.That(result.Status, Is.EqualTo("200 OK"));
        }

        [Test]
        public void FileServer_returns_403_on_directory_traversal_attempt()
        {
            
            var result = AppUtils.Call(fileServer.Invoke, "/../ccinfo.txt");

            Assert.That(result.Status, Is.EqualTo("403 Forbidden"));
        }

        [Test]
        public void FileServer_returns_correct_byte_range_in_body()
        {
            var result = AppUtils.CallPipe(b => b.Run(fileServer.Invoke),
                FakeHostRequest.GetRequest("/test.txt", req =>
                    req.Headers["Range"] = "bytes=22-33"));

            Assert.That(result.Status, Is.EqualTo("206 Partial Content"));
            Assert.That(result.Headers["Content-Length"], Is.EqualTo("12"));
            Assert.That(result.Headers["Content-Range"], Is.EqualTo("bytes 22-33/193"));
            Assert.That(result.BodyText, Is.EqualTo("-*- test -*-"));
        }

        [Test]
        public void FileServer_returns_error_for_unsatisfiable_byte_range()
        {
            var result = AppUtils.CallPipe(b => b.Run(fileServer.Invoke),
                FakeHostRequest.GetRequest("/test.txt", req =>
                    req.Headers["Range"] = "bytes=1234-5678"));
            
            Assert.That(result.Status, Is.EqualTo("416 Requested Range Not Satisfiable"));
            Assert.That(result.Headers["Content-Range"], Is.EqualTo("bytes */193"));
        }
    }
}
