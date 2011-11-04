using System;
using Gate.Builder;
using NUnit.Framework;
using Gate.Owin;
using Gate;
using Gate.Middleware;
using Gate.TestHelpers;

namespace Gate.Middleware.Tests
{
    [TestFixture]
    public class PredicateTests
    {
        FakeHostResponse CallPipeline(Action<IAppBuilder> pipe)
        {
            return AppUtils.Call(AppBuilder.BuildConfiguration(pipe));
        }

        Action<IAppBuilder> ResponseWithStatus(string status)
        {
            return b => b.Run(AppUtils.Simple(status, null, null));
        }

        [Test]
        public void Where_falls_through_when_predicate_is_false()
        {
            var result = CallPipeline(b =>
                b.Where((e, c) => c(false), b0 => b0.Run(AppUtils.Simple("predicate was true", null, null)))
                .Run(AppUtils.Simple("predicate was false", null, null)));

            Assert.That(result.Status, Is.EqualTo("predicate was false"));
        }

        [Test]
        public void Where_forks_when_predicate_is_true()
        {
            var result = CallPipeline(b =>
                b.Where((e, c) => c(true), b0 => b0.Run(AppUtils.Simple("predicate was true", null, null)))
                .Run(AppUtils.Simple("predicate was false", null, null)));

            Assert.That(result.Status, Is.EqualTo("predicate was true"));
        }

        [Test]
        public void Unless_falls_through_when_predicate_is_true()
        {
            var result = CallPipeline(b =>
                b.Unless((e, c) => c(true), b0 => b0.Run(AppUtils.Simple("predicate was false", null, null)))
                .Run(AppUtils.Simple("predicate was true", null, null)));

            Assert.That(result.Status, Is.EqualTo("predicate was true"));
        }

        [Test]
        public void Unless_forks_when_predicate_is_false()
        {
            var result = CallPipeline(b =>
                b.Unless((e, c) => c(false), b0 => b0.Run(AppUtils.Simple("predicate was false", null, null)))
                .Run(AppUtils.Simple("predicate was true", null, null)));

            Assert.That(result.Status, Is.EqualTo("predicate was false"));
        }
    }
}

