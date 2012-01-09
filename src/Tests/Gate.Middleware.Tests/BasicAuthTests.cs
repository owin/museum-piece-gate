using System;
using System.Collections.Generic;
using Gate.Builder;
using NUnit.Framework;
using Gate.Middleware;
using Gate.TestHelpers;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class BasicAuthTests
    {
        [Test]
        public void GetBasicAuth_returns_null_if_no_auth_header()
        {
            var e = new Environment() { Headers = Headers.New() };
            Assert.That(e.GetBasicAuth(), Is.Null);
        }

        [Test]
        public void GetBasicAuth_return_null_if_auth_header_value_isnt_Basic()
        {
            var e = new Environment() { Headers = Headers.New().SetHeader("authorization", "Foo") };
            Assert.That(e.GetBasicAuth(), Is.Null);
        }

        [Test]
        public void GetBasicAuth_decodes_basic_auth()
        {
            var e = new Environment() { Headers = Headers.New().SetHeader("authorization", "Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==") };
            Assert.That(e.GetBasicAuth(), Is.EqualTo("Aladdin:open sesame"));
        }

        [Test]
        public void BasicAuth_falls_through_if_authenticated()
        {
            var result = AppUtils.CallPipe(b =>
                b.BasicAuth((e, c) => c(true), "RealmString")
                .Simple("200 OK"));

            Assert.That(result.Status, Is.EqualTo("200 OK"));
        }

        [Test]
        public void BasicAuth_returns_401_if_not_authenticated()
        {
            var result = AppUtils.CallPipe(b =>
                b.BasicAuth((e, c) => c(false), "RealmString")
                .Simple("200 OK"));

            Assert.That(result.Status, Is.EqualTo("401 Authorization Required"));
            Assert.That(result.Headers.GetHeader("WWW-Authenticate"), Is.EqualTo("Basic Realm=\"RealmString\""));
        }
    }
}

