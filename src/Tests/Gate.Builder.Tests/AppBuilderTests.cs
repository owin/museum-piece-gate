using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gate.Builder;
using Owin;
using Gate.TestHelpers;
using NUnit.Framework;
using Shouldly;
using Xunit;

namespace Gate.Builder.Tests
{
#pragma warning disable 811
    using AppFunc = Func< // Call
        IDictionary<string, object>, // Environment
        IDictionary<string, string[]>, // Headers
        Stream, // Body
        CancellationToken, // CallCancelled
        Task<Tuple< //Result
            IDictionary<string, object>, // Properties
            int, // Status
            IDictionary<string, string[]>, // Headers
            Func< // CopyTo
                Stream, // Body
                CancellationToken, // CopyToCancelled
                Task>>>>; // Done

    public class AppBuilderTests
    {
        // ReSharper disable InconsistentNaming
        static readonly AppDelegate TwoHundredFoo = (call, cancel) => TaskHelpers.FromResult(
            new ResultParameters
            {
                Status = 200,
                Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    {"Content-Type", new[] {"text/plain"}}
                },
                Body = (stream, stop) =>
                {
                    stream.Write(Encoding.UTF8.GetBytes("Hello Foo"), 0, 9);
                    return TaskHelpers.Completed();
                }
            });

        static readonly AppFunc TwoHundredFooAction = (env, headers, body, cancel) => TaskHelpers.FromResult(
            new Tuple<IDictionary<string, object>, int, IDictionary<string, string[]>, Func<Stream, CancellationToken, Task>>(
                new Dictionary<string, object>(),
                200,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    {"Content-Type", new[] {"text/plain"}}
                },
                (stream, stop) =>
                {
                    stream.Write(Encoding.UTF8.GetBytes("Hello Foo"), 0, 9);
                    return TaskHelpers.Completed();
                }
                ));

        AppDelegate Build(Action<IAppBuilder> b)
        {
            return AppBuilder.BuildPipeline<AppDelegate>(b);
        }

        [Fact]
        public Task Build_returns_404_by_default()
        {
            var builder = new AppBuilder();
            var app = builder.Materialize<AppDelegate>();
            var client = TestHttpClient.ForAppDelegate(app);

            return client.GetAsync("http://localhost")
                .Then(response => response.StatusCode.ShouldBe(HttpStatusCode.NotFound));
        }

        [Fact]
        public Task Calling_use_produces_app_that_is_returned_by_Build()
        {
            var client = TestHttpClient.ForConfiguration(
                builder => builder.Use<AppDelegate>(_ => TwoHundredFoo));

            return client.GetAsync("http://localhost")
                .Then(response =>
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    return response.Content.ReadAsStringAsync()
                        .Then(body => body.ShouldBe("Hello Foo"));
                });
        }

        //[Test]
        //public void Extension_method_for_Run_lets_you_pass_in_AppDelegate_instead_of_AppDelegate_factory()
        //{
        //    var app = Build(b => b
        //        .Run(TwoHundredFoo));

        //    var stat = "";
        //    app(null, (status, headers, body) => stat = status, ex => { });
        //    Assert.That(stat, Is.EqualTo("200 Foo"));
        //}

        //public static void MyConfig(IAppBuilder builder)
        //{
        //    builder.Run(TwoHundredFoo);
        //}

        //[Test]
        //public void Calling_Configure_passes_control_to_a_builder_configuration_method()
        //{
        //    var app = Build(MyConfig);

        //    var stat = "";
        //    app(null, (status, headers, body) => stat = status, ex => { });
        //    Assert.That(stat, Is.EqualTo("200 Foo"));
        //}

        //[Test]
        //public void Overloaded_constructor_calls_Configure()
        //{
        //    var app = Build(MyConfig);

        //    var stat = "";
        //    app(null, (status, headers, body) => stat = status, ex => { });
        //    Assert.That(stat, Is.EqualTo("200 Foo"));
        //}

        //static string Execute(AppDelegate app)
        //{
        //    var stat = "";
        //    app(null, (status, headers, body) => stat = status, null);
        //    return stat;
        //}

        //static AppDelegate ReturnStatus(string status)
        //{
        //    return (env, result, fault) => result(status, null, null);
        //}

        //static AppDelegate AppendStatus(AppDelegate app, string text)
        //{
        //    return (env, result, fault) =>
        //        app(
        //            env,
        //            (status, headers, body) =>
        //                result(status + text, headers, body),
        //            fault);
        //}

        //[Test]
        //public void Extension_methods_let_you_call_factories_with_parameters()
        //{
        //    var app = Build(b => b
        //        .Run(ReturnStatus, "200 Foo"));

        //    var status = Execute(app);
        //    Assert.That(status, Is.EqualTo("200 Foo"));
        //}

        //[Test]
        //public void Calling_Use_wraps_middleware_around_app()
        //{
        //    var app = Build(b => b
        //        .Use(AppendStatus, "[2]")
        //        .Run(ReturnStatus, "[1]")
        //        );
        //    var status = Execute(app);
        //    Assert.That(status, Is.EqualTo("[1][2]"));
        //}

        //[Test]
        //public void Use_middleware_runs_in_the_order_they_are_registered()
        //{
        //    var app = Build(b => b
        //        .Use(AppendStatus, "[3]")
        //        .Use(AppendStatus, "[2]")
        //        .Run(ReturnStatus, "[1]")
        //        );

        //    var status = Execute(app);
        //    Assert.That(status, Is.EqualTo("[1][2][3]"));
        //}

        //[Test]
        //public void Class_with_IApplication_can_be_used_by_AppBuilder()
        //{
        //    var withApplication = new WithApplication();
        //    var app = Build(b => b
        //        .Run(withApplication.App)
        //        );
        //    var callResult = AppUtils.Call(app);
        //    Assert.That(callResult.Status, Is.EqualTo("200 WithApplication"));
        //}

        //[Test]
        //public void Class_with_IApplication_can_have_parameters()
        //{
        //    var withApplication = new WithApplication();
        //    var app = Build(b => b
        //        .Run(withApplication.App, "200 WithApplication", "Foo!")
        //        );
        //    var callResult = AppUtils.Call(app);
        //    Assert.That(callResult.Status, Is.EqualTo("200 WithApplication"));
        //}




        //[Test]
        //public void Class_with_IMiddleware_can_be_used_by_AppBuilder()
        //{
        //    var withMiddleware = new WithMiddleware();
        //    var app = Build(b => b
        //        .Use(withMiddleware.Middleware)
        //        .Run(AppUtils.Simple("200 OK", Headers.New(), "Hello world"))
        //        );
        //    var callResult = AppUtils.Call(app);
        //    Assert.That(callResult.Status, Is.EqualTo("200 OKWithMiddleware"));
        //}

        //[Test]
        //public void Class_with_IMiddleware_can_have_parameters()
        //{
        //    var withMiddleware2 = new WithMiddleware();
        //    var app = Build(b => b
        //        .Use(withMiddleware2.Middleware, "AppendCustom")
        //        .Run(AppUtils.Simple("200 OK", Headers.New(), "Hello world"))
        //        );
        //    var callResult = AppUtils.Call(app);
        //    Assert.That(callResult.Status, Is.EqualTo("200 OKAppendCustom"));
        //}

        //static readonly Func<AppAction, string, AppAction> AddStatus = delegate(AppAction app, string append)
        //{
        //    return (env, result, fault) =>
        //        app(
        //            env,
        //            (status, headers, body) =>
        //                result(status + append, headers, body),
        //            fault);
        //};

        //[Test]
        //public void AppBuilder_has_action_overloads_to_support_pure_system_namespace_delegates()
        //{
        //    var app = Build(b => b
        //        .Run(TwoHundredFooAction)
        //        );

        //    Assert.That(app, Is.Not.Null);
        //    var result = AppUtils.Call(app);
        //    Assert.That(result.Status, Is.EqualTo("200 Foo"));
        //    Assert.That(result.BodyText, Is.EqualTo("Hello Foo"));
        //}

        //[Test]
        //public void AppBuilder_has_action_overloads_which_support_use_and_parameters()
        //{
        //    var app = Build(b => b
        //        .Use(AddStatus, "Yarg")
        //        .Run(TwoHundredFooAction)
        //        );

        //    Assert.That(app, Is.Not.Null);
        //    var result = AppUtils.Call(app);
        //    Assert.That(result.Status, Is.EqualTo("200 FooYarg"));
        //    Assert.That(result.BodyText, Is.EqualTo("Hello Foo"));
        //}

        //[Test]
        //public void Use_middleware_inside_calls_to_map_only_apply_to_requests_that_go_inside_map()
        //{
        //    var app = Build(b => b
        //        .Use(AddStatus, " Outer")
        //        .Map("/here", map => map
        //            .Use(AddStatus, " Mapped")
        //            .Run(TwoHundredFoo))
        //        .Use(AddStatus, " Inner")
        //        .Run(TwoHundredFoo)
        //        );

        //    var resultThere = AppUtils.Call(app, "/there");
        //    var resultHere = AppUtils.Call(app, "/here");

        //    Assert.That(resultThere.Status, Is.EqualTo("200 Foo Inner Outer"));
        //    Assert.That(resultHere.Status, Is.EqualTo("200 Foo Mapped Outer"));
        //}

        //[Test]
        //public void Use_middleware_between_calls_to_map_only_apply_to_requests_that_reach_later_maps()
        //{
        //    var builder = new AppBuilder();
        //    builder
        //        .Use(AddStatus, " Outer")
        //        .Map("/here1", map => map
        //            .Use(AddStatus, " Mapped1")
        //            .Run(TwoHundredFoo))
        //        .Use(AddStatus, " Between")
        //        .Map("/here2", map => map
        //            .Use(AddStatus, " Mapped2")
        //            .Run(TwoHundredFoo))
        //        .Use(AddStatus, " Inner")
        //        .Run(TwoHundredFoo);

        //    var app = builder.Materialize();

        //    var resultThere = AppUtils.Call(app, "/there");
        //    var resultHere1 = AppUtils.Call(app, "/here1");
        //    var resultHere2 = AppUtils.Call(app, "/here2");

        //    Assert.That(resultThere.Status, Is.EqualTo("200 Foo Inner Between Outer"));
        //    Assert.That(resultHere1.Status, Is.EqualTo("200 Foo Mapped1 Outer"));
        //    Assert.That(resultHere2.Status, Is.EqualTo("200 Foo Mapped2 Between Outer"));
        //}

    }

    //internal class WithApplication
    //{
    //    public AppDelegate App()
    //    {
    //        return App("200 WithApplication", "Hello World");
    //    }

    //    public AppDelegate App(string status, string content)
    //    {
    //        return AppUtils.Simple(status, Headers.New(), content);
    //    }
    //}

    //internal class WithMiddleware
    //{
    //    public AppDelegate Middleware(AppDelegate app)
    //    {
    //        return Middleware(app, "WithMiddleware");
    //    }

    //    public AppDelegate Middleware(AppDelegate app, string appendStatus)
    //    {
    //        return (env, result, fault) =>
    //            app(
    //                env,
    //                (status, headers, body) =>
    //                    result(status + appendStatus, headers, body),
    //                fault);
    //    }
    //}
}