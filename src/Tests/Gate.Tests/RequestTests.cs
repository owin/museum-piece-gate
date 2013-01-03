using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using Owin;
using System.Threading;

namespace Gate.Tests
{
    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class RequestTests
    {
        private IDictionary<string, object> CreateEmptyEnvironment()
        {
            return new Dictionary<string, object>()
            {
                { "owin.RequestHeaders",  Headers.New() },
                { "owin.ResponseHeaders", Headers.New() },
            };
        }
        private IDictionary<string, object> CreateEmptyEnvironment(
            Action<IDictionary<string, object>> setupEnv,
            Action<IDictionary<string, string[]>> setupHeaders)
        {
            var env = CreateEmptyEnvironment();
            setupEnv(env);
            setupHeaders(env.Get<IDictionary<string, string[]>>("owin.RequestHeaders"));
            return env;
        }

        [Test]
        public void QueryString_is_used_to_populate_Query_dictionary()
        {
            var request = new Request(CreateEmptyEnvironment()) { QueryString = "foo=bar" };
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void Changing_QueryString_in_environment_reparses_Query_dictionary()
        {
            var request = new Request(CreateEmptyEnvironment()) { QueryString = "foo=bar" };
            Assert.That(request.Query["foo"], Is.EqualTo("bar"));

            request.QueryString = "foo=quux";
            Assert.That(request.Query["foo"], Is.EqualTo("quux"));
        }

        //[Test]
        //public void Body_is_used_to_populate_Post_dictionary()
        //{
        //    var request = new Request(CreateEmptyCall()) { Method = "POST", BodyDelegate = Body.FromText("foo=bar") };
        //    Assert.That(request.Post["foo"], Is.EqualTo("bar"));
        //}

        //[Test]
        //public void Changing_Body_in_environment_reparses_Post_dictionary()
        //{
        //    var request = new Request(CreateEmptyCall()) { Method = "POST", BodyAction = Body.FromText("foo=bar") };
        //    Assert.That(request.Post["foo"], Is.EqualTo("bar"));

        //    request.BodyAction = Body.FromText("foo=quux");
        //    Assert.That(request.Post["foo"], Is.EqualTo("quux"));
        //}

        [Test]
        public void Host_will_remove_port_from_request_header_if_needed()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Host", "Beta:8080");
            var request = new Request(env);
            Assert.That(request.Host, Is.EqualTo("Beta"));
        }

        [Test]
        public void Host_is_based_on_server_ip_if_header_is_missing()
        {
            var env = CreateEmptyEnvironment(e => e.Set("server.LocalIpAddress", "1.2.3.4"), h => { });
            var request = new Request(env);
            Assert.That(request.Host, Is.EqualTo("1.2.3.4"));
        }

        [Test]
        public void Port_will_take_port_from_request_header_if_needed()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Host", "Beta:8080");
            var request = new Request(env);
            Assert.That(request.Port, Is.EqualTo(8080));
        }

        [Test]
        public void Port_is_based_on_localport_if_not_part_of_request_header()
        {
            var env = CreateEmptyEnvironment(
                e => e.Set("server.LocalPort", "55"),
                h => h.SetHeader("Host", "Beta"));
            var request = new Request(env);
            Assert.That(request.Port, Is.EqualTo(55));
        }
        [Test]
        public void Port_is_based_on_scheme_if_there_is_no_local_port()
        {
            var env = CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https"),
                h => h.SetHeader("Host", "Beta"));
            var request = new Request(env);
            Assert.That(request.Port, Is.EqualTo(443));
        }
        [Test]
        public void Port_default_to_80()
        {
            var env = CreateEmptyEnvironment(
                e => { },
                h => h.SetHeader("Host", "Beta"));
            var request = new Request(env);
            Assert.That(request.Port, Is.EqualTo(80));
        }


        [Test]
        public void ContentType_and_MediaType_should_return_http_header()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Content-Type", "text/plain");
            var request = new Request(env);
            Assert.That(request.ContentType, Is.EqualTo("text/plain"));
            Assert.That(request.MediaType, Is.EqualTo("text/plain"));
        }

        [Test]
        public void MediaType_is_shorter_when_delimited()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Content-Type", "text/html; charset=utf-8");
            var request = new Request(env);
            Assert.That(request.ContentType, Is.EqualTo("text/html; charset=utf-8"));
            Assert.That(request.MediaType, Is.EqualTo("text/html"));
        }

        [Test]
        public void ContentType_and_MediaType_are_null_when_missing()
        {
            var env = CreateEmptyEnvironment();
            var request = new Request(env);
            Assert.That(request.ContentType, Is.Null);
            Assert.That(request.MediaType, Is.Null);
        }

        [Test]
        public void ItShouldParseCookies()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Cookie", "foo=bar;quux=h&m");
            var request = new Request(env);
            Assert.That(request.Cookies["foo"], Is.EqualTo("bar"));
            Assert.That(request.Cookies["quux"], Is.EqualTo("h&m"));
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").Remove("Cookie");
            Assert.That(request.Cookies.Count, Is.EqualTo(0));
        }

        [Test]
        public void ItShouldAlwaysReturnTheSameDictionaryObject()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Cookie", "foo=bar;quux=h&m");
            var request = new Request(env);
            var cookies = request.Cookies;
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").Remove("Cookie");
            Assert.That(request.Cookies, Is.SameAs(cookies));
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Cookie", "zoo=m");
            Assert.That(request.Cookies, Is.SameAs(cookies));
        }

        [Test]
        public void ItShouldModifyTheCookiesDictionaryInPlace()
        {
            var env = CreateEmptyEnvironment();
            var request = new Request(env);
            var cookies = request.Cookies;
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").Remove("Cookie");
            Assert.That(cookies.Count, Is.EqualTo(0));
            cookies["foo"] = "bar";
            Assert.That(cookies.Count, Is.EqualTo(1));
            Assert.That(request.Cookies["foo"], Is.EqualTo("bar"));
        }


        [Test]
        public void ItShouldParseCookiesAccordingToRFC2109()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders").SetHeader("Cookie", "foo=bar;foo=car");
            var request = new Request(env);
            Assert.That(request.Cookies["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void ItShouldParseCookiesWithQuotes()
        {
            var env = CreateEmptyEnvironment();
            env.Get<IDictionary<string, string[]>>("owin.RequestHeaders")
                .SetHeader("Cookie", @"$Version=""1""; Customer=""WILE_E_COYOTE""; $Path=""/acme""; Part_Number=""Rocket_Launcher_0001""; $Path=""/acme""");
            var request = new Request(env);

            Assert.That(request.Cookies["$Version"], Is.EqualTo(@"""1"""));
            Assert.That(request.Cookies["Customer"], Is.EqualTo(@"""WILE_E_COYOTE"""));
            Assert.That(request.Cookies["$Path"], Is.EqualTo(@"""/acme"""));
            Assert.That(request.Cookies["Part_Number"], Is.EqualTo(@"""Rocket_Launcher_0001"""));
        }

        [Test]
        public void QueryShouldDecodePlusAsSpace()
        {
            var req = Request.Create();
            req.QueryString = "foo=hello+world&the+bar=quux";
            Assert.That(req.Query["foo"], Is.EqualTo("hello world"));
            Assert.That(req.Query["the bar"], Is.EqualTo("quux"));
        }

        [Test]
        public void EverythingWillFallbackToLoopback80()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => { },
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("127.0.0.1:80"));
            Assert.That(req.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(req.Port, Is.EqualTo(80));
        }
        [Test]
        public void HostHeaderMayBeIpv4()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => { },
                h => h.SetHeader("Host", "1.2.3.4:5678")));

            Assert.That(req.HostWithPort, Is.EqualTo("1.2.3.4:5678"));
            Assert.That(req.Host, Is.EqualTo("1.2.3.4"));
            Assert.That(req.Port, Is.EqualTo(5678));
        }
        [Test]
        public void HostHeaderMayBeIpv4WithoutPort()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => { },
                h => h.SetHeader("Host", "1.2.3.4")));

            Assert.That(req.HostWithPort, Is.EqualTo("1.2.3.4"));
            Assert.That(req.Host, Is.EqualTo("1.2.3.4"));
            Assert.That(req.Port, Is.EqualTo(80));
        }
        [Test]
        public void HostHeaderMayBeIpv6()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => { },
                h => h.SetHeader("Host", "[::1234]:5678")));

            Assert.That(req.HostWithPort, Is.EqualTo("[::1234]:5678"));
            Assert.That(req.Host, Is.EqualTo("::1234"));
            Assert.That(req.Port, Is.EqualTo(5678));
        }
        [Test]
        public void HostHeaderMayBeIpv6WithoutPort()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => { },
                h => h.SetHeader("Host", "[::1]")));

            Assert.That(req.HostWithPort, Is.EqualTo("[::1]"));
            Assert.That(req.Host, Is.EqualTo("::1"));
            Assert.That(req.Port, Is.EqualTo(80));
        }
        [Test]
        public void HostHeaderMayBeIpv6WithoutBraces()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => { },
                h => h.SetHeader("Host", "2001:0db8:0000:0000:0000:ff00:0042:8329")));

            Assert.That(req.HostWithPort, Is.EqualTo("2001:0db8:0000:0000:0000:ff00:0042:8329"));
            Assert.That(req.Host, Is.EqualTo("2001:db8::ff00:42:8329"));
            Assert.That(req.Port, Is.EqualTo(80));
        }
        [Test]
        public void HostHeaderMayBeDns()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => { },
                h => h.SetHeader("Host", "www.example.com:5678")));

            Assert.That(req.HostWithPort, Is.EqualTo("www.example.com:5678"));
            Assert.That(req.Host, Is.EqualTo("www.example.com"));
            Assert.That(req.Port, Is.EqualTo(5678));
        }
        [Test]
        public void HostHeaderMayBeDnsWithoutPort()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => { },
                h => h.SetHeader("Host", "www.example.com")));

            Assert.That(req.HostWithPort, Is.EqualTo("www.example.com"));
            Assert.That(req.Host, Is.EqualTo("www.example.com"));
            Assert.That(req.Port, Is.EqualTo(80));
        }
        [Test]
        public void PortShouldFallbackToLocalPortFirst()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https").Set("server.LocalPort", "54321"),
                h => h.SetHeader("Host", "www.example.com")));

            Assert.That(req.HostWithPort, Is.EqualTo("www.example.com"));
            Assert.That(req.Host, Is.EqualTo("www.example.com"));
            Assert.That(req.Port, Is.EqualTo(54321));
        }
        [Test]
        public void PortShouldFallbackToSchemePortSecond()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https"),
                h => h.SetHeader("Host", "www.example.com")));

            Assert.That(req.HostWithPort, Is.EqualTo("www.example.com"));
            Assert.That(req.Host, Is.EqualTo("www.example.com"));
            Assert.That(req.Port, Is.EqualTo(443));
        }
        [Test]
        public void PortWithoutHeaderShouldFallbackToLocalPortFirst()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https")
                    .Set("server.LocalPort", "54321"),
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("127.0.0.1:54321"));
            Assert.That(req.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(req.Port, Is.EqualTo(54321));
        }
        [Test]
        public void PortWithoutHeaderShouldFallbackToSchemePortSecond()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https"),
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("127.0.0.1:443"));
            Assert.That(req.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(req.Port, Is.EqualTo(443));
        }

        [Test]
        public void HostShouldFallbackLocalIpv4Address()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https")
                    .Set("server.LocalIpAddress", "1.2.3.4"),
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("1.2.3.4:443"));
            Assert.That(req.Host, Is.EqualTo("1.2.3.4"));
            Assert.That(req.Port, Is.EqualTo(443));
        }
        [Test]
        public void HostShouldFallbackLocalIpv6Address()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https")
                    .Set("server.LocalIpAddress", "2001:db8::ff00:42:8329"),
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("[2001:db8::ff00:42:8329]:443"));
            Assert.That(req.Host, Is.EqualTo("2001:db8::ff00:42:8329"));
            Assert.That(req.Port, Is.EqualTo(443));
        }
        [Test]
        public void HostShouldFallbackLocalIpv4AddressWithLocalPort()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https")
                    .Set("server.LocalIpAddress", "1.2.3.4")
                    .Set("server.LocalPort", "54321"),
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("1.2.3.4:54321"));
            Assert.That(req.Host, Is.EqualTo("1.2.3.4"));
            Assert.That(req.Port, Is.EqualTo(54321));
        }
        [Test]
        public void HostShouldFallbackLocalIpv6AddressWithLocalPort()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https")
                    .Set("server.LocalIpAddress", "2001:db8::ff00:42:8329")
                    .Set("server.LocalPort", "54321"),
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("[2001:db8::ff00:42:8329]:54321"));
            Assert.That(req.Host, Is.EqualTo("2001:db8::ff00:42:8329"));
            Assert.That(req.Port, Is.EqualTo(54321));
        }

        [Test]
        public void PortShouldBeAssignable()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https")
                    .Set("server.LocalIpAddress", "2001:db8::ff00:42:8329")
                    .Set("server.LocalPort", "54321"),
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("[2001:db8::ff00:42:8329]:54321"));
            Assert.That(req.Host, Is.EqualTo("2001:db8::ff00:42:8329"));
            Assert.That(req.Port, Is.EqualTo(54321));

            req.Port = 86;
            Assert.That(req.HostWithPort, Is.EqualTo("[2001:db8::ff00:42:8329]:86"));
            Assert.That(req.Host, Is.EqualTo("2001:db8::ff00:42:8329"));
            Assert.That(req.Port, Is.EqualTo(86));
        }

        [Test]
        public void HostShouldBeAssignable()
        {
            var req = new Request(CreateEmptyEnvironment(
                e => e.Set("owin.RequestScheme", "https")
                    .Set("server.LocalIpAddress", "2001:db8::ff00:42:8329")
                    .Set("server.LocalPort", "54321"),
                h => { }));

            Assert.That(req.HostWithPort, Is.EqualTo("[2001:db8::ff00:42:8329]:54321"));
            Assert.That(req.Host, Is.EqualTo("2001:db8::ff00:42:8329"));
            Assert.That(req.Port, Is.EqualTo(54321));

            req.Host = "localhost";
            Assert.That(req.HostWithPort, Is.EqualTo("localhost:54321"));
            Assert.That(req.Host, Is.EqualTo("localhost"));
            Assert.That(req.Port, Is.EqualTo(54321));

            req.Host = "1.2.3.4";
            Assert.That(req.HostWithPort, Is.EqualTo("1.2.3.4:54321"));
            Assert.That(req.Host, Is.EqualTo("1.2.3.4"));
            Assert.That(req.Port, Is.EqualTo(54321));

            req.Host = "2001:db8::ff00:42:8329";
            Assert.That(req.HostWithPort, Is.EqualTo("[2001:db8::ff00:42:8329]:54321"));
            Assert.That(req.Host, Is.EqualTo("2001:db8::ff00:42:8329"));
            Assert.That(req.Port, Is.EqualTo(54321));
        }

        [Test]
        public void IsSerializable()
        {
            var request = new Request(CreateEmptyEnvironment());
            var serializer = new BinaryFormatter();
            Assert.DoesNotThrow(() => serializer.Serialize(new MemoryStream(), request));
        }
    }
}