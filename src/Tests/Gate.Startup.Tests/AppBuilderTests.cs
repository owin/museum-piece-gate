using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Gate.Startup.Tests
{
    using AppDelegate = Action< // app
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


    [TestFixture]
    public class AppBuilderTests
    {
        // ReSharper disable InconsistentNaming

        public static AppDelegate TwoHundredFoo = (env, result, fault) => result("200 Foo", null, null);

        [Test]
        public void Build_returns_null_by_default()
        {
            var builder = new AppBuilder();
            var app = builder.Build();
            Assert.That(app, Is.Null);
        }

        [Test]
        public void Calling_Run_with_factory_produces_app_that_is_returned_by_Build()
        {
            var app = new AppBuilder()
                .Run(() => TwoHundredFoo)
                .Build();
            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
        }

        [Test]
        public void Extension_method_for_Run_lets_you_pass_in_AppDelegate_instead_of_AppDelegate_factory()
        {
            var app = new AppBuilder()
                .Run(TwoHundredFoo)
                .Build();
            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
        }

        public static void MyConfig(AppBuilder builder)
        {
            builder.Run(TwoHundredFoo);
        }

        [Test]
        public void Calling_Configure_passes_control_to_a_builder_configuration_method()
        {
            var app = new AppBuilder()
                .Configure(MyConfig)
                .Build();
            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
        }

        [Test]
        public void Overloaded_constructor_calls_Configure()
        {
            var app = new AppBuilder(MyConfig).Build();
            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
        }

        public void NoWay(AppBuilder builder)
        {
            builder.Run((a, b, c) => b("200 Way", null, null));
        }

        [Test]
        public void String_constructor_overload_also_eventually_calls_Configure()
        {
            var builder = new AppBuilder("Gate.Startup.Tests.AppBuilderTests.NoWay");
            var app = builder.Build();
            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Way"));
        }


        static string Execute(AppDelegate app)
        {
            var stat = "";
            app(null, (status, headers, body) => stat = status, null);
            return stat;
        }

        static AppDelegate ReturnStatus(string status)
        {
            return (env, result, fault) => result(status, null, null);
        }

        static AppDelegate AppendStatus(AppDelegate app, string text)
        {
            return (env, result, fault) =>
                app(
                    env,
                    (status, headers, body) =>
                        result(status + text, headers, body),
                    fault);
        }

        [Test]
        public void Extension_methods_let_you_call_factories_with_parameters()
        {
            var app = new AppBuilder()
                .Run(ReturnStatus, "200 Foo")
                .Build();

            var status = Execute(app);
            Assert.That(status, Is.EqualTo("200 Foo"));
        }

        [Test]
        public void Calling_Use_wraps_middleware_around_app()
        {
            var app = new AppBuilder()
                .Use(AppendStatus, "[2]")
                .Run(ReturnStatus, "[1]")
                .Build();
            var status = Execute(app);
            Assert.That(status, Is.EqualTo("[1][2]"));
        }

        [Test]
        public void Use_middleware_runs_in_the_order_they_are_registered()
        {
            var app = new AppBuilder()
                .Use(AppendStatus, "[3]")
                .Use(AppendStatus, "[2]")
                .Run(ReturnStatus, "[1]")
                .Build();

            var status = Execute(app);
            Assert.That(status, Is.EqualTo("[1][2][3]"));
        }
    }
}