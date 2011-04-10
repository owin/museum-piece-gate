using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Helpers.Tests
{
    [TestFixture]
    public class NotFoundTests
    {
        [Test]
        public void Not_found_returns_status_404()
        {
            var app = NotFound.Create();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("404 NOTFOUND"));
        }
    }
}
