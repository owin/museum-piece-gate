using System;
using System.Collections.Generic;
using System.Text;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Tests.StartupTests
{
    using AppAction = Action< // app
        IDictionary<string, object>, // env
        Action< // result
            string, // status
            IDictionary<string, string>, // headers
            Func< // body
                Func< // next
                    ArraySegment<byte>, // data
                    Action, // continuation
                    bool>, // async                    
                Action<Exception>, // error
                Action, // complete
                Action>>, // cancel
        Action<Exception>>; // error

    public class AppBuilderExtTests
    {
        // ReSharper disable InconsistentNaming

        static readonly AppAction TwoHundredFoo = (env, result, fault) => result(
            "200 Foo",
            new Dictionary<string, string> {{"Content-Type", "text/plain"}},
            (next, error, complete) =>
            {
                next(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello Foo")), null);
                complete();
                return () => { };
            });

        static AppAction AddStatus(AppAction app, string append)
        {
            return (env, result, fault) =>
                app(
                    env,
                    (status, headers, body) =>
                        result(status + append, headers, body),
                    fault);
        }

        [Test]
        public void Ext_property_has_methods_to_support_pure_system_namespace_delegates()
        {
            var builder = new AppBuilder();
            var app = builder
                .Run(TwoHundredFoo)
                .Build();

            var result = AppUtils.Call(app);
            Assert.That(result.Status, Is.EqualTo("200 Foo"));
            Assert.That(result.BodyText, Is.EqualTo("Hello Foo"));
        }

        [Test]
        public void Ext_property_supports_use_and_parameters()
        {
            var builder = new AppBuilder();
            var app = builder
                .Use(AddStatus, "Yarg")
                .Run(TwoHundredFoo)
                .Build();

            var result = AppUtils.Call(app);
            Assert.That(result.Status, Is.EqualTo("200 FooYarg"));
            Assert.That(result.BodyText, Is.EqualTo("Hello Foo"));
        }
    }
}