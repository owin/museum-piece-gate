using Gate.Helpers;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Tests.Helpers
{
    [TestFixture]
    public class NotFoundTests
    {
        [Test]
        public void Not_found_returns_status_404()
        {
            var app = NotFound.Create();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("404 NotFound"));
        }
    }
}