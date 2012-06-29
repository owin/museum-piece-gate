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
    using AppAction = Func< // Call
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

    using ResultTuple = Tuple<IDictionary<string, object>, int, IDictionary<string, string[]>, Func<Stream, CancellationToken, Task>>;

    public class AppBuilderTests
    {
        // ReSharper disable InconsistentNaming
        static readonly AppDelegate TwoHundredFoo = call => TaskHelpers.FromResult(
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
                },
                Properties = new Dictionary<string, object>
                {
                    {"owin.ReasonPhrase", "Foo"}
                }
            });

        static readonly AppAction TwoHundredFooAction = (env, headers, body, cancel) => TaskHelpers.FromResult(
            new ResultTuple(
                new Dictionary<string, object>
                {
                    {"owin.ReasonPhrase", "Foo"}
                },
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
            var client = TestHttpClient.ForConfiguration(builder => builder.Use<AppDelegate>(_ => TwoHundredFoo));

            return client.GetAsync("http://localhost")
                .Then(response =>
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    response.ReasonPhrase.ShouldBe("Foo");
                    return response.Content.ReadAsStringAsync()
                        .Then(body => body.ShouldBe("Hello Foo"));
                });
        }

        [Fact]
        public Task Extension_method_for_Run_lets_you_pass_in_AppDelegate_instead_of_AppDelegate_factory()
        {
            var client = TestHttpClient.ForConfiguration(builder => builder.Run(TwoHundredFoo));

            return client.GetAsync("http://localhost")
                .Then(response =>
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    response.ReasonPhrase.ShouldBe("Foo");
                    return response.Content.ReadAsStringAsync()
                        .Then(body => body.ShouldBe("Hello Foo"));
                });
        }

        public static void MyConfig(IAppBuilder builder)
        {
            builder.Run(TwoHundredFoo);
        }

        [Fact]
        public Task Calling_BiuldPipeline_passes_control_to_a_builder_configuration_method()
        {
            var app = AppBuilder.BuildPipeline<AppDelegate>(MyConfig);

            var client = TestHttpClient.ForAppDelegate(app);

            return client.GetAsync("http://localhost")
                .Then(response =>
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    response.ReasonPhrase.ShouldBe("Foo");
                    return response.Content.ReadAsStringAsync()
                        .Then(body => body.ShouldBe("Hello Foo"));
                });
        }


        //static string Execute(AppDelegate app)
        //{
        //    var stat = "";
        //    app(null, (status, headers, body) => stat = status, null);
        //    return stat;
        //}

        static AppDelegate ReturnStatus(int status, string reasonPhrase)
        {
            return call => TaskHelpers.FromResult(new ResultParameters
            {
                Properties = new Dictionary<string, object> { { "owin.ReasonPhrase", reasonPhrase } },
                Status = status,
                Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            });
        }

        static AppDelegate AppendStatus(AppDelegate app, string reasonPhrase)
        {
            return call => app(call).Then(result =>
            {
                result.Properties["owin.ReasonPhrase"] = result.Properties["owin.ReasonPhrase"] + reasonPhrase;
                return result;
            });
        }

        [Fact]
        public Task Extension_methods_let_you_call_factories_with_parameters()
        {
            var client = TestHttpClient.ForConfiguration(builder => builder.Run(ReturnStatus, 201, "Foo"));

            return client.GetAsync("http://localhost")
               .Then(response =>
               {
                   response.StatusCode.ShouldBe(HttpStatusCode.Created);
                   response.ReasonPhrase.ShouldBe("Foo");
               });
        }

        [Fact]
        public Task Calling_Use_wraps_middleware_around_app()
        {
            var client = TestHttpClient.ForConfiguration(builder => builder
                .Use(AppendStatus, "[2]")
                .Run(ReturnStatus, 200, "[1]"));

            return client.GetAsync("http://localhost")
                .Then(response =>
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.OK);
                    response.ReasonPhrase.ShouldBe("[1][2]");
                });
        }

        [Fact]
        public Task Use_middleware_runs_in_the_order_they_are_registered()
        {
            var client = TestHttpClient.ForConfiguration(builder => builder
                .Use(AppendStatus, "[3]")
                .Use(AppendStatus, "[2]")
                .Run(ReturnStatus, 201, "[1]"));

            return client.GetAsync("http://localhost")
                .Then(response =>
                {
                    response.StatusCode.ShouldBe(HttpStatusCode.Created);
                    response.ReasonPhrase.ShouldBe("[1][2][3]");
                });
        }


        static readonly Func<AppAction, string, AppAction> AddStatus =
            (app, appendReasonPhrase) =>
                (env, headers, body, cancel) =>
                    app(env, headers, body, cancel).Then(result =>
                    {
                        result.Item1["owin.ReasonPhrase"] = result.Item1["owin.ReasonPhrase"] + appendReasonPhrase;
                        return result;
                    });

        [Fact]
        public Task AppBuilder_has_action_overloads_to_support_pure_system_namespace_delegates()
        {
            var client = TestHttpClient.ForConfiguration(b => b
                .Run(TwoHundredFooAction)
                );

            return client.GetAsync("http://localhost")
               .Then(response =>
               {
                   response.StatusCode.ShouldBe(HttpStatusCode.OK);
                   response.ReasonPhrase.ShouldBe("Foo");
               });
        }

        [Fact]
        public Task AppBuilder_has_action_overloads_which_support_use_and_parameters()
        {
            var client = TestHttpClient.ForConfiguration(b => b
                .Use(AddStatus, "Yarg")
                .Run(TwoHundredFooAction)
                );


            return client.GetAsync("http://localhost")
               .Then(response =>
               {
                   response.StatusCode.ShouldBe(HttpStatusCode.OK);
                   response.ReasonPhrase.ShouldBe("FooYarg");
               });
        }

        [Fact]
        public Task Use_middleware_inside_calls_to_map_only_apply_to_requests_that_go_inside_map()
        {
            var client = TestHttpClient.ForConfiguration(b => b
                .Use(AddStatus, " Outer")
                .Map("/here", map => map
                    .Use(AddStatus, " Mapped")
                    .Run(TwoHundredFoo))
                .Use(AddStatus, " Inner")
                .Run(TwoHundredFoo)
                );

            return client.GetAsync("http://localhost/there").Then(resultThere =>
                    client.GetAsync("http://localhost/here").Then(resultHere =>
                    {
                        resultThere.ReasonPhrase.ShouldBe("Foo Inner Outer");
                        resultHere.ReasonPhrase.ShouldBe("Foo Mapped Outer");
                    }));
        }

        [Fact]
        public Task Use_middleware_between_calls_to_map_only_apply_to_requests_that_reach_later_maps()
        {
            var client = TestHttpClient.ForConfiguration(builder =>
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
                    .Run(TwoHundredFoo));


            return client.GetAsync("http://localhost/there").Then(resultThere =>
                client.GetAsync("http://localhost/here1").Then(resultHere1 =>
                    client.GetAsync("http://localhost/here2").Then(resultHere2 =>
                    {
                        resultThere.ReasonPhrase.ShouldBe("Foo Inner Between Outer");
                        resultHere1.ReasonPhrase.ShouldBe("Foo Mapped1 Outer");
                        resultHere2.ReasonPhrase.ShouldBe("Foo Mapped2 Between Outer");
                    })));
        }

    }

}