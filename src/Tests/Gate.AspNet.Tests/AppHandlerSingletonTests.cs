using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Gate.AspNet.Tests
{
    [TestFixture]
    public class AppHandlerSingletonTests
    {
        [Test]
        public void Factory_method_should_be_called_once()
        {
            var handler = new AppHandler(null);
            var calls = 0;
            AppHandlerSingleton.SetFactory(() =>
            {
                ++calls;
                return handler;
            });
            Assert.That(calls, Is.EqualTo(0));

            var handler1 = AppHandlerSingleton.Instance;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(handler1, Is.SameAs(handler));

            var handler2 = AppHandlerSingleton.Instance;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(handler2, Is.SameAs(handler));
        }

        [Test]
        public void Reassigning_factory_will_call_it_again()
        {
            var handler = new AppHandler(null);
            var calls = 0;
            Func<AppHandler> factory = () =>
            {
                ++calls;
                return handler;
            };

            AppHandlerSingleton.SetFactory(factory);
            Assert.That(calls, Is.EqualTo(0));

            var handler1 = AppHandlerSingleton.Instance;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(handler1, Is.SameAs(handler));

            var handler2 = AppHandlerSingleton.Instance;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(handler2, Is.SameAs(handler));

            AppHandlerSingleton.SetFactory(factory);
            Assert.That(calls, Is.EqualTo(1));

            var handler3 = AppHandlerSingleton.Instance;
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(handler3, Is.SameAs(handler));

            var handler4 = AppHandlerSingleton.Instance;
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(handler4, Is.SameAs(handler));
        }
    }
}
