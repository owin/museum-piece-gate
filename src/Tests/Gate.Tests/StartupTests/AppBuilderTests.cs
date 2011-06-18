using System.Collections.Generic;
using Gate.TestHelpers;
using NUnit.Framework;

namespace Gate.Tests.StartupTests
{
    [TestFixture]
    public class AppBuilderTests
    {
        // ReSharper disable InconsistentNaming
        static readonly AppDelegate TwoHundredFoo = (env, result, fault) => result("200 Foo", null, null);

        [Test]
        public void Build_returns_404_by_default()
        {
            var builder = new AppBuilder();
            var app = builder.Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("404 Not Found"));
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
            var builder = new AppBuilder();
            MyConfig(builder);
            var app = builder.Build();

            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
        }

        [Test]
        public void Overloaded_constructor_calls_Configure()
        {
            var builder = new AppBuilder();
            MyConfig(builder);
            var app = builder.Build();

            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
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

        [Test]
        public void Class_with_IApplication_can_be_used_by_AppBuilder()
        {
            var withIApplication = new WithIApplication();
            var app = new AppBuilder()
                .Run(withIApplication.Create)
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 WithIApplication"));
        }

        [Test]
        public void Class_with_IApplication_can_have_parameters()
        {
            var withIApplication2 = new WithIApplication();
            var app = new AppBuilder()
                .Run(withIApplication2.Create, "200 WithIApplication", "Foo!")
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 WithIApplication"));
        }

        [Test]
        public void Run_extension_methods_enable_you_to_provide_type_instead_of_create_instance()
        {
            var app = new AppBuilder()
                .Run<WithIApplication>()
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 WithIApplication"));
        }

        [Test]
        public void Run_extension_methods_for_type_also_accept_parameters()
        {
            var app = new AppBuilder()
                .Run<WithIApplication, string, string>("200 CustomStatus", "Foo!")
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 CustomStatus"));
        }

        [Test]
        public void Run_extension_method_with_extra_call_to_take_parameters()
        {
            var app = new AppBuilder()
                .WithArgs("200 CustomStatus2", "Foo!").Run<WithIApplication>()
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 CustomStatus2"));
        }

        [Test]
        public void Class_with_IMiddleware_can_be_used_by_AppBuilder()
        {
            var withIMiddleware = new WithIMiddleware();
            var app = new AppBuilder()
                .Use(withIMiddleware.Create)
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello world"))
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 OKWithIMiddleware"));
        }

        [Test]
        public void Class_with_IMiddleware_can_have_parameters()
        {
            var withIMiddleware2 = new WithIMiddleware();
            var app = new AppBuilder()
                .Use(withIMiddleware2.Create, "AppendCustom")
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello world"))
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 OKAppendCustom"));
        }

        [Test]
        public void Use_extension_methods_enable_you_to_provide_type_instead_of_create_instance()
        {
            var app = new AppBuilder()
                .Use<WithIMiddleware>()
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello world"))
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 OKWithIMiddleware"));
        }

        [Test]
        public void Use_extension_methods_for_type_also_accept_parameters()
        {
            var app = new AppBuilder()
                .Use<WithIMiddleware, string>("CustomStatus")
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello world"))
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 OKCustomStatus"));
        }

        [Test]
        public void Use_extension_method_with_extra_call_to_take_parameters()
        {
            var app = new AppBuilder()
                .WithArgs("CustomStatus2").Use<WithIMiddleware>()
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello world"))
                .Build();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 OKCustomStatus2"));
        }
    }

    internal class WithIApplication : IApplication, IApplication<string, string>
    {
        public AppDelegate Create()
        {
            return Create("200 WithIApplication", "Hello World");
        }

        public AppDelegate Create(string status, string content)
        {
            return AppUtils.Simple(status, new Dictionary<string, string>(), content);
        }
    }

    internal class WithIMiddleware : IMiddleware, IMiddleware<string>
    {
        public AppDelegate Create(AppDelegate app)
        {
            return Create(app, "WithIMiddleware");
        }

        public AppDelegate Create(AppDelegate app, string appendStatus)
        {
            return (env, result, fault) =>
                app(
                    env,
                    (status, headers, body) =>
                        result(status + appendStatus, headers, body),
                    fault);
        }
    }
}