﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Owin;
using System.IO;
using System.Threading.Tasks;

namespace Gate.Middleware.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using System.Threading;

    [TestFixture]
    public class PassiveValidatorTests
    {
        private string ReadBody(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(stream,Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        [Test]
        public void NormalPassThrough_Success()
        {
            AppFunc middleware = new PassiveValidatorMiddleware(
                env =>
                {
                    env[OwinConstants.ResponseStatusCode] = 200;
                    return TaskHelpers.Completed();
                }).Invoke;

            Request request = Request.Create();
            request.CancellationToken = new CancellationTokenSource().Token;
            request.Body = Stream.Null;
            request.Method = "GET";
            request.Path = "/foo";
            request.PathBase = string.Empty;
            request.Protocol = "HTTP/1.1";
            request.QueryString = "foo=bar";
            request.Scheme = "http";
            request.Version = "1.0";
            request.HostWithPort = "hostname:8080";

            request.Environment[OwinConstants.ResponseBody] = new MemoryStream();

            middleware(request.Environment).Wait();

            Response response = new Response(request.Environment);            

            Assert.That(response.StatusCode, Is.EqualTo(200), ReadBody(response.OutputStream));
            Assert.That(response.Headers.GetHeader("X-OwinValidatorWarning"), Is.Null);
        }
    }
}
