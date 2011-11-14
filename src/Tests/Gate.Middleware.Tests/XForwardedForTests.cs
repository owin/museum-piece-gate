using System;
using NUnit;
using System.Collections.Generic;
using Gate;
using NUnit.Framework;
using System.Linq;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class XForwardedForTests
    {
        IDictionary<string, object> dict;
     
        [SetUp]
        public void SetUp()
        {
            dict = new Dictionary<string, object>();
        }
     
        public void SetXFF(string xForwardedFor)
        {
            new Environment(dict).Headers = new Dictionary<string, string>() { { "x-forwarded-for", xForwardedFor } };
        }

        [Test]
        public void Remote_address_is_empty_if_no_xff()
        {
            var host = dict.GetRemoteAddress();
            Assert.That(host, Is.EqualTo(""));
        }

        [Test]
        public void Remote_address_is_empty_if_empty_xff()
        {
            SetXFF("");
            var host = dict.GetRemoteAddress();
            Assert.That(host, Is.EqualTo(""));
        }

        [Test]
        public void Remote_address_is_single_xff_value()
        {
            SetXFF("4.4.4.4");
            var host = dict.GetRemoteAddress();
            Assert.That(host, Is.EqualTo("4.4.4.4"));
        }

        [Test]
        public void Remote_address_is_right_most_value()
        {
            SetXFF("1.2.3.4, 4.4.4.4");
            var host = dict.GetRemoteAddress();
            Assert.That(host, Is.EqualTo("4.4.4.4"));
        }

        [Test]
        public void Proxied_addresses_are_empty_if_no_xff()
        {
            var host = dict.GetProxiedAddresses();
            Assert.That(host, Is.EqualTo(""));
        }

        [Test]
        public void Proxied_addresses_are_empty_if_empty_xff()
        {
            SetXFF("");
            var host = dict.GetProxiedAddresses();
            Assert.That(host, Is.EqualTo(""));
        }

        [Test]
        public void Proxied_addresses_are_empty_if_single_xff()
        {
            SetXFF("4.4.4.4");
            var host = dict.GetProxiedAddresses();
            Assert.That(host, Is.EqualTo(""));
        }

        [Test]
        public void Proxied_addresses_is_left_most_value()
        {
            SetXFF("1.2.3.4, 4.4.4.4");
            var host = dict.GetProxiedAddresses();
            Assert.That(host, Is.EqualTo(new [] { "1.2.3.4" }));
        }

        [Test]
        public void Proxied_addresses_are_left_most_values()
        {
            SetXFF("4.3.2.1, 1.2.3.4, 4.4.4.4");
            var host = dict.GetProxiedAddresses();
            Assert.That(host, Is.EqualTo(new[] { "4.3.2.1", "1.2.3.4" }));
        }

        [Test]
        public void Proxied_addresses_skips_known_proxies()
        {
            SetXFF("4.3.2.1, 1.2.3.4, 4.4.4.4");
            var host = dict.GetProxiedAddresses("1.2.3.4");
            Assert.That(host, Is.EqualTo(new[] { "4.3.2.1" }));
        }

        [Test]
        public void Proxied_addresses_skips_multiple_known_proxies()
        {
            SetXFF("4.3.2.1, 5.5.5.5, 1.2.3.4, 4.4.4.4");
            var host = dict.GetProxiedAddresses("1.2.3.4", "5.5.5.5");
            Assert.That(host, Is.EqualTo(new[] { "4.3.2.1" }));
        }

        [Test]
        public void Proxied_addresses_skips_known_proxies_and_returns_multiple_unknown_hops()
        {
            SetXFF("1.1.1.1, 4.3.2.1, 1.2.3.4, 4.4.4.4");
            var host = dict.GetProxiedAddresses("1.2.3.4");
            Assert.That(host, Is.EqualTo(new[] { "1.1.1.1", "4.3.2.1" }));
        }

        [Test]
        public void Proxied_addresses_skips_multiple_known_proxies_and_returns_multiple_unknown_hops()
        {
            SetXFF("1.1.1.1, 4.3.2.1, 5.5.5.5, 1.2.3.4, 4.4.4.4");
            var host = dict.GetProxiedAddresses("1.2.3.4", "5.5.5.5");
            Assert.That(host, Is.EqualTo(new[] { "1.1.1.1", "4.3.2.1" }));
        }

        [Test]
        public void Proxied_addresses_includes_unknown_proxies()
        {
            SetXFF("1.1.1.1, 4.3.2.1, 5.5.5.5, 6.6.6.6, 1.2.3.4, 4.4.4.4");
            var host = dict.GetProxiedAddresses("1.2.3.4", "5.5.5.5");
            Assert.That(host, Is.EqualTo(new[] { "1.1.1.1", "4.3.2.1", "5.5.5.5", "6.6.6.6" }));
        }
	}
}
