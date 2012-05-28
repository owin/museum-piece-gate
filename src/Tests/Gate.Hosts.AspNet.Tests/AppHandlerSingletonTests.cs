using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Owin;

namespace Gate.Hosts.AspNet.Tests
{
    [TestFixture]
    public class AppHandlerSingletonTests
    {
        [Test]
        public void Factory_method_should_be_called_once()
        {
            AppDelegate handler = (env,result,fault)=> { };
            var calls = 0;
            AppSingleton.SetFactory(() =>
            {
                ++calls;
                return handler;
            });
            Assert.That(calls, Is.EqualTo(0));

            var handler1 = AppSingleton.Instance;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(handler1, Is.SameAs(handler));

            var handler2 = AppSingleton.Instance;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(handler2, Is.SameAs(handler));
        }

        [Test]
        public void Reassigning_factory_will_call_it_again()
        {
            AppDelegate handler = (env, result, fault) => { };
            var calls = 0;
            Func<AppDelegate> factory = () =>
            {
                ++calls;
                return handler;
            };

            AppSingleton.SetFactory(factory);
            Assert.That(calls, Is.EqualTo(0));

            var handler1 = AppSingleton.Instance;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(handler1, Is.SameAs(handler));

            var handler2 = AppSingleton.Instance;
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(handler2, Is.SameAs(handler));

            AppSingleton.SetFactory(factory);
            Assert.That(calls, Is.EqualTo(1));

            var handler3 = AppSingleton.Instance;
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(handler3, Is.SameAs(handler));

            var handler4 = AppSingleton.Instance;
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(handler4, Is.SameAs(handler));
        }
    }
}
