using System;
using Gate.Middleware;
using Gate.TestHelpers;
using Gate.Owin;
using Gate.Builder;
using NUnit.Framework;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class ContentLengthTests
    {
        [Test]
        public void Content_length_is_not_added_if_body_is_null()
        {
            var result = AppUtils.CallPipe(b => b
                .ContentLength()
                .Simple("200 OK"));

            Assert.That(result.Headers.ContainsKey("content-length"), Is.False);
        }

        [Test]
        public void Content_length_is_added_if_body_is_zero_length() // ???
        {
            BodyDelegate body = (onNext, onError, onComplete) => { onComplete(); return null; };
            var result = AppUtils.CallPipe(b => b
                .ContentLength()
                .Simple("200 OK", body));

            Assert.That(result.Headers["content-length"], Is.EqualTo("0"));
        }

        [Test]
        public void Content_length_is_not_changed()
        {
            var headers = AppUtils.CreateHeaderDictionary();
            headers["content-length"] = "69";

            var result = AppUtils.CallPipe(b => b
                .ContentLength()
                .Simple("200 OK", headers, (onNext, onError, onComplete) => { onComplete(); return null; }));

            Assert.That(result.Headers["content-length"], Is.EqualTo("69"));
        }

        [Test]
        public void Content_length_is_not_added_if_transfer_encoding_is_present()
        {
            var headers = AppUtils.CreateHeaderDictionary();
            headers["transfer-encoding"] = "chunked";

            var result = AppUtils.CallPipe(b => b
                .ContentLength()
                .Simple("200 OK", headers, (onNext, onError, onComplete) => { onComplete(); return null; }));

            Assert.That(result.Headers.ContainsKey("content-length"), Is.False);
        }

        [Test]
        [Sequential]
        public void Content_length_is_not_added_if_response_status_should_or_possibly_must_not_have_one_of_a_set_of_http_statuses(
            [Values("204 No Content", "205 Reset Content")] string status)
        {
            var result = AppUtils.CallPipe(b => b
                .ContentLength()
                .Simple(status, AppUtils.CreateHeaderDictionary(), (onNext, onError, onComplete) => { onComplete(); return null; }));

            Assert.That(result.Headers.ContainsKey("content-length"), Is.False);
        }
    }
}

