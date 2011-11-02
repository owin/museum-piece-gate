using System;
using System.Text;
using System.Collections.Generic;
using Gate.Builder;
using Gate.Owin;
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

    [TestFixture]
    public class AppBuilderTests
    {
        // ReSharper disable InconsistentNaming
        static readonly AppDelegate TwoHundredFoo = (env, result, fault) => result(
            "200 Foo",
            new Dictionary<string, string> { { "Content-Type", "text/plain" } },
            (next, error, complete) =>
            {
                next(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello Foo")), null);
                complete();
                return () => { };
            });

        static readonly AppAction TwoHundredFooAction = (env, result, fault) => result(
            "200 Foo",
            new Dictionary<string, string> { { "Content-Type", "text/plain" } },
            (next, error, complete) =>
            {
                next(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello Foo")), null);
                complete();
                return () => { };
            });

        AppDelegate Build(Action<IAppBuilder> b)
        {
            return AppBuilder.BuildConfiguration(b);
        }

        [Test]
        public void Build_returns_404_by_default()
        {
            var builder = new AppBuilder();
            var app = builder.Materialize();
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("404 Not Found"));
        }

        [Test]
        public void Calling_Run_with_factory_produces_app_that_is_returned_by_Build()
        {
            var app = Build(b =>
                b.Run(() => TwoHundredFoo));
            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
        }

        [Test]
        public void Extension_method_for_Run_lets_you_pass_in_AppDelegate_instead_of_AppDelegate_factory()
        {
            var app = Build(b => b
                .Run(TwoHundredFoo));

            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
        }

        public static void MyConfig(IAppBuilder builder)
        {
            builder.Run(TwoHundredFoo);
        }

        [Test]
        public void Calling_Configure_passes_control_to_a_builder_configuration_method()
        {
            var app = Build(MyConfig);

            var stat = "";
            app(null, (status, headers, body) => stat = status, ex => { });
            Assert.That(stat, Is.EqualTo("200 Foo"));
        }

        [Test]
        public void Overloaded_constructor_calls_Configure()
        {
            var app = Build(MyConfig);

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
            var app = Build(b => b
                .Run(ReturnStatus, "200 Foo"));

            var status = Execute(app);
            Assert.That(status, Is.EqualTo("200 Foo"));
        }

        [Test]
        public void Calling_Use_wraps_middleware_around_app()
        {
            var app = Build(b => b
                .Use(AppendStatus, "[2]")
                .Run(ReturnStatus, "[1]")
                );
            var status = Execute(app);
            Assert.That(status, Is.EqualTo("[1][2]"));
        }

        [Test]
        public void Use_middleware_runs_in_the_order_they_are_registered()
        {
            var app = Build(b => b
                .Use(AppendStatus, "[3]")
                .Use(AppendStatus, "[2]")
                .Run(ReturnStatus, "[1]")
                );

            var status = Execute(app);
            Assert.That(status, Is.EqualTo("[1][2][3]"));
        }

        [Test]
        public void Class_with_IApplication_can_be_used_by_AppBuilder()
        {
            var withApplication = new WithApplication();
            var app = Build(b => b
                .Run(withApplication.App)
                );
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 WithApplication"));
        }

        [Test]
        public void Class_with_IApplication_can_have_parameters()
        {
            var withApplication = new WithApplication();
            var app = Build(b => b
                .Run(withApplication.App, "200 WithApplication", "Foo!")
                );
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 WithApplication"));
        }




        [Test]
        public void Class_with_IMiddleware_can_be_used_by_AppBuilder()
        {
            var withMiddleware = new WithMiddleware();
            var app = Build(b => b
                .Use(withMiddleware.Middleware)
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello world"))
                );
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 OKWithMiddleware"));
        }

        [Test]
        public void Class_with_IMiddleware_can_have_parameters()
        {
            var withMiddleware2 = new WithMiddleware();
            var app = Build(b => b
                .Use(withMiddleware2.Middleware, "AppendCustom")
                .Run(AppUtils.Simple("200 OK", new Dictionary<string, string>(), "Hello world"))
                );
            var callResult = AppUtils.Call(app);
            Assert.That(callResult.Status, Is.EqualTo("200 OKAppendCustom"));
        }

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
        public void AppBuilder_has_action_overloads_to_support_pure_system_namespace_delegates()
        {
            var app = Build(b => b
                .Run(TwoHundredFooAction)
                );

            Assert.That(app, Is.Not.Null);
            var result = AppUtils.Call(app);
            Assert.That(result.Status, Is.EqualTo("200 Foo"));
            Assert.That(result.BodyText, Is.EqualTo("Hello Foo"));
        }

        [Test]
        public void AppBuilder_has_action_overloads_which_support_use_and_parameters()
        {
            var app = Build(b => b
                .Use(AddStatus, "Yarg")
                .Run(TwoHundredFooAction)
                );

            Assert.That(app, Is.Not.Null);
            var result = AppUtils.Call(app);
            Assert.That(result.Status, Is.EqualTo("200 FooYarg"));
            Assert.That(result.BodyText, Is.EqualTo("Hello Foo"));
        }

        [Test]
        public void Use_middleware_inside_calls_to_map_only_apply_to_requests_that_go_inside_map()
        {
            var app = Build(b => b
                .Use(AddStatus, " Outer")
                .Map("/here", map => map
                    .Use(AddStatus, " Mapped")
                    .Run(TwoHundredFoo))
                .Use(AddStatus, " Inner")
                .Run(TwoHundredFoo)
                );

            var resultThere = AppUtils.Call(app, "/there");
            var resultHere = AppUtils.Call(app, "/here");

            Assert.That(resultThere.Status, Is.EqualTo("200 Foo Inner Outer"));
            Assert.That(resultHere.Status, Is.EqualTo("200 Foo Mapped Outer"));
        }

        [Test]
        public void Use_middleware_between_calls_to_map_only_apply_to_requests_that_reach_later_maps()
        {
            var builder = new AppBuilder();
            builder
                .Use(AddStatus, " Outer")
                .Map("/here1", map => map
                    .Use(AddStatus, " Mapped1")
                    .Run(TwoHundredFoo))
                .Use(AddStatus, " Between")
                .Map("/here2", map => map
                    .Use(AddStatus, " Mapped2")
                    .Run(TwoHundredFoo))
                .Use(AddStatus, " Inner")
                .Run(TwoHundredFoo);
            
            var app = builder.Materialize();

            var resultThere = AppUtils.Call(app, "/there");
            var resultHere1 = AppUtils.Call(app, "/here1");
            var resultHere2 = AppUtils.Call(app, "/here2");

            Assert.That(resultThere.Status, Is.EqualTo("200 Foo Inner Between Outer"));
            Assert.That(resultHere1.Status, Is.EqualTo("200 Foo Mapped1 Outer"));
            Assert.That(resultHere2.Status, Is.EqualTo("200 Foo Mapped2 Between Outer"));
        }

    }

    internal class WithApplication
    {
        public AppDelegate App()
        {
            return App("200 WithApplication", "Hello World");
        }

        public AppDelegate App(string status, string content)
        {
            return AppUtils.Simple(status, new Dictionary<string, string>(), content);
        }
    }

    internal class WithMiddleware
    {
        public AppDelegate Middleware(AppDelegate app)
        {
            return Middleware(app, "WithMiddleware");
        }

        public AppDelegate Middleware(AppDelegate app, string appendStatus)
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