using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Owin;
using NUnit.Framework;

namespace Gate.Tests
{
    [TestFixture]
    public class ResponseTests
    {
        private IDictionary<string, object> CreateEmptyEnvironment()
        {
            return new Dictionary<string, object>()
            {
                { "owin.RequestHeaders",  Headers.New() },
                { "owin.ResponseHeaders", Headers.New() },
            };
        }

        [Test]
        public void Finish_will_call_result_delegate_with_current_status_and_headers()
        {
            var env = CreateEmptyEnvironment();
            var response = new Response(env)
            {
                Status = "200 Blah",
                ContentType = "text/blah",
            };

            Assert.That(env.Get<int>("owin.ResponseStatusCode"), Is.EqualTo(200));
            Assert.That(env.Get<string>("owin.ResponseReasonPhrase"), Is.EqualTo("Blah"));
            Assert.That(env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders").GetHeader("Content-Type"), Is.EqualTo("text/blah"));
        }

        // TODO: Re-work when Buffering is added to Response.
        [Test]
        public void Write_calls_will_spool_until_finish_is_called()
        {
            var env = CreateEmptyEnvironment();
            MemoryStream buffer = new MemoryStream();
            env.Set("owin.ResponseBody", buffer);
            var resp = new Response(env) { Status = "200 Yep" };
            resp.Write("this");
            resp.Write("is");
            resp.Write("a");
            resp.Write("test");

            Assert.That(env.Get<int>("owin.ResponseStatusCode"), Is.EqualTo(200));

            buffer.Seek(0, SeekOrigin.Begin);
            var data = Encoding.UTF8.GetString(buffer.ToArray());
            Assert.That(data, Is.EqualTo("thisisatest"));
        }

        [Test]
        public void ItCanSetCookies()
        {
            var response = new Response(CreateEmptyEnvironment());
            response.SetCookie("foo", "bar");
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[] { "foo=bar; path=/" }));
            response.SetCookie("foo2", "bar2");
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[] { "foo=bar; path=/", "foo2=bar2; path=/" }));
            response.SetCookie("foo3", "bar3");
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[] { "foo=bar; path=/", "foo2=bar2; path=/", "foo3=bar3; path=/" }));
        }

        [Test]
        public void ItCanSetCookiesWithTheSameNameForMultipleDomains()
        {
            var response = new Response(CreateEmptyEnvironment());
            response.SetCookie("foo", new Response.Cookie { Value = "bar", Domain = "sample.example.com" });
            response.SetCookie("foo", new Response.Cookie { Value = "bar", Domain = ".example.com" });
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[]
            {
                "foo=bar; domain=sample.example.com; path=/", 
                "foo=bar; domain=.example.com; path=/"
            }));
        }

        [Test]
        public void ItFormatsTheCookieExpirationDataAccordinglyToRfc2109()
        {
            var response = new Response(CreateEmptyEnvironment());
            response.SetCookie("foo", new Response.Cookie { Value = "bar", Expires = new DateTime(1971, 10, 14, 12, 34, 56) });
            Assert.That(response.GetHeader("Set-Cookie"), Is.StringMatching(@"expires=..., \d\d-...-\d\d\d\d \d\d:\d\d:\d\d ..."));
        }

        [Test]
        public void ItCanSetSecureCookies()
        {
            var response = new Response(CreateEmptyEnvironment());
            response.SetCookie("foo", new Response.Cookie { Value = "bar", Secure = true });
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[] { @"foo=bar; path=/; secure" }));
        }


        [Test]
        public void ItCanSetHttpOnlyCookies()
        {
            var response = new Response(CreateEmptyEnvironment());
            response.SetCookie("foo", new Response.Cookie { Value = "bar", HttpOnly = true });
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[] { @"foo=bar; path=/; HttpOnly" }));
        }

        [Test]
        public void ItCanDeleteCookies()
        {
            var response = new Response(CreateEmptyEnvironment());
            response.SetCookie("foo", "bar");
            response.SetCookie("foo2", "bar2");
            response.DeleteCookie("foo");
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[] { "foo2=bar2; path=/", "foo=; expires=Thu, 01-Jan-1970 00:00:00 GMT" }));
        }

        [Test]
        public void ItCanDeleteCookiesWithTheSameNameFromMultipleDomains()
        {
            var response = new Response(CreateEmptyEnvironment());
            response.SetCookie("foo", new Response.Cookie { Value = "bar", Domain = "sample.example.com" });
            response.SetCookie("foo", new Response.Cookie { Value = "bar", Domain = ".example.com" });
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[]
            {
                "foo=bar; domain=sample.example.com; path=/", 
                "foo=bar; domain=.example.com; path=/"
            }));
            response.DeleteCookie("foo", new Response.Cookie { Domain = ".example.com" });
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[]
            {
                "foo=bar; domain=sample.example.com; path=/", 
                "foo=; domain=.example.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT"
            }));
            response.DeleteCookie("foo", new Response.Cookie { Domain = "sample.example.com" });
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[] 
            { 
                "foo=; domain=.example.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT",
                "foo=; domain=sample.example.com; path=/; expires=Thu, 01-Jan-1970 00:00:00 GMT" 
            }));
        }


        [Test]
        public void ItCanDeleteCookiesWithTheSameNameWithDifferentPaths()
        {
            var response = new Response(CreateEmptyEnvironment());
            response.SetCookie("foo", "bar");
            response.SetCookie("foo", new Response.Cookie { Value = "bar", Path = "/path" });
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[]
            {
                "foo=bar; path=/",
                "foo=bar; path=/path"
            }));
            response.DeleteCookie("foo", new Response.Cookie { Path = "/path" });
            Assert.That(response.GetHeaders("Set-Cookie"), Is.EquivalentTo(new[]
            {
                "foo=bar; path=/",
                "foo=; path=/path; expires=Thu, 01-Jan-1970 00:00:00 GMT"
            }));
        }
    }
}