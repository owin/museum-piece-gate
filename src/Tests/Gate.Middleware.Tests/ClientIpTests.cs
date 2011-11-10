using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Middleware.Tests
{
    public class ClientIpTests
    {
        static FakeHostResponse CallPipeline(string forwardedFor, string clientIp, params string[] knownHttpProxies)
        {
            return AppUtils.CallPipe(
                builder => builder.Use(ClientIp.Middleware, knownHttpProxies).Run(AppUtils.ShowEnvironment),
                request =>
                {
                    if (clientIp != null)
                        request["server.CLIENT_IP"] = clientIp;
                    if (forwardedFor != null)
                        request.Headers["X-Forwarded-For"] = forwardedFor;
                });
        }

        static void AssertResultValues(FakeHostResponse result, string expectedForwardedFor, string expectedClientIp)
        {
            var elt = result.BodyXml.Element("server.CLIENT_IP");
            var clientIp = elt == null ? null : elt.Value;


            elt = result.BodyXml.Element("headers").Element("X-Forwarded-For");
            var forwardedFor = elt == null ? null : elt.Value;

            Assert.That(clientIp, Is.EqualTo(expectedClientIp));
            Assert.That(forwardedFor, Is.EqualTo(expectedForwardedFor));
        }


        [Test]
        public void ClientIp_and_ForwardedFor_are_unchanged_when_ClientIp_is_present()
        {
            var result = CallPipeline("6.5.7.8, 9.10.11.12", "1.2.3.4");
            AssertResultValues(result, "6.5.7.8, 9.10.11.12", "1.2.3.4");
        }

        [Test]
        public void Missing_ForwardedFor_remains_absent_when_ClientIp_is_present()
        {
            var result = CallPipeline(null, "1.2.3.4");
            AssertResultValues(result, null, "1.2.3.4");
        }

        [Test]
        public void Final_component_of_ForwardedFor_becomes_ClientIp_when_absent_empty_or_whitespace()
        {
            AssertResultValues(CallPipeline("6.5.7.8, 9.10.11.12", null), "6.5.7.8", "9.10.11.12");
            AssertResultValues(CallPipeline("6.5.7.8, 9.10.11.12", ""), "6.5.7.8", "9.10.11.12");
            AssertResultValues(CallPipeline("6.5.7.8, 9.10.11.12", " "), "6.5.7.8", "9.10.11.12");
        }

        [Test]
        public void ForwardedFor_is_removed_when_only_value_becomes_ClientIp()
        {
            var result = CallPipeline("6.5.7.8", null);
            AssertResultValues(result, null, "6.5.7.8");
        }

        [Test]
        public void Multiple_request_headers_will_also_act_as_delimiter()
        {
            var result = CallPipeline("6.5.7.8\r\n9.10.11.12", null);
            AssertResultValues(result, "6.5.7.8", "9.10.11.12");
        }

        [Test]
        public void Known_proxy_address_is_removed_from_client_ip()
        {
            var result = CallPipeline("6.5.7.8", "127.0.0.1", "127.0.0.1");
            AssertResultValues(result, null, "6.5.7.8");
        }
        [Test]
        public void Multiple_proxy_addresses_are_pulled_as_long_as_they_are_recognized()
        {
            var result = CallPipeline("6.5.7.8, 9.10.11.12, 13.14.15.16", "127.0.0.1", "127.0.0.1", "9.10.11.12", "13.14.15.16");
            AssertResultValues(result, null, "6.5.7.8");
        }
        [Test]
        public void Spoofed_remote_ips_are_left_on_the_header_when_supposed_proxies_are_not_recognized()
        {
            var result = CallPipeline("255.255.255.255, 6.5.7.8, 9.10.11.12, 13.14.15.16", "127.0.0.1", "127.0.0.1", "9.10.11.12", "13.14.15.16");
            AssertResultValues(result, "255.255.255.255", "6.5.7.8");
        }
    }
}
