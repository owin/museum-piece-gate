using System;
using System.Collections.Generic;
using System.Linq;
using DifferentNamespace;
using Gate.Startup;
using Gate.Startup.Loader;
using NUnit.Framework;

namespace Gate.Tests.StartupTests.Loader
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
        Action<Exception>>; // fault

    [TestFixture]
    public class DefaultConfigurationLoaderTests
    {
        [Test]
        public void Strings_are_split_based_on_dots()
        {
            var strings = DefaultConfigurationLoader.DotByDot("this.is.a.test").ToArray();
            Assert.That(strings.Length, Is.EqualTo(4));
            Assert.That(strings[0], Is.EqualTo("this.is.a.test"));
            Assert.That(strings[1], Is.EqualTo("this.is.a"));
            Assert.That(strings[2], Is.EqualTo("this.is"));
            Assert.That(strings[3], Is.EqualTo("this"));
        }

        [Test]
        public void Leading_and_trailing_dot_and_empty_strings_are_safe_and_ignored()
        {
            var string1 = DefaultConfigurationLoader.DotByDot(".a.test").ToArray();
            var string2 = DefaultConfigurationLoader.DotByDot("a.test.").ToArray();
            var string3 = DefaultConfigurationLoader.DotByDot(".a.test.").ToArray();
            var string4 = DefaultConfigurationLoader.DotByDot(".").ToArray();
            var string5 = DefaultConfigurationLoader.DotByDot("").ToArray();
            var string6 = DefaultConfigurationLoader.DotByDot(null).ToArray();

            AssertArrayEqual(string1, new[] {"a.test", "a"});
            AssertArrayEqual(string2, new[] {"a.test", "a"});
            AssertArrayEqual(string3, new[] {"a.test", "a"});
            AssertArrayEqual(string4, new string[0]);
            AssertArrayEqual(string5, new string[0]);
            AssertArrayEqual(string6, new string[0]);
        }

        void AssertArrayEqual(string[] arr1, string[] arr2)
        {
            Assert.That(arr1.Length, Is.EqualTo(arr2.Length));
            foreach (var index in Enumerable.Range(0, arr1.Length))
            {
                Assert.That(arr1[index], Is.EqualTo(arr2[index]));
            }
        }

        static int _helloCalls;

        public static void Hello(AppBuilder builder)
        {
            _helloCalls += 1;
        }

        [Test]
        public void Load_will_find_assembly_and_type_and_static_method()
        {
            var loader = new DefaultConfigurationLoader();
            var configuration = loader.Load("Gate.Tests.StartupTests.Loader.DefaultConfigurationLoaderTests.Hello");

            _helloCalls = 0;
            configuration(null);
            Assert.That(_helloCalls, Is.EqualTo(1));
        }

        [Test]
        public void An_extra_segment_will_cause_the_match_to_fail()
        {
            var loader = new DefaultConfigurationLoader();
            var configuration = loader.Load("Gate.Tests.StartupTests.Loader.DefaultConfigurationLoaderTests.Hello.Bar");

            Assert.That(configuration, Is.Null);
        }

        [Test]
        public void Calling_a_class_with_multiple_configs_is_okay()
        {
            var loader = new DefaultConfigurationLoader();
            var foo = loader.Load("Gate.Tests.StartupTests.Loader.MultiConfigs.Foo");
            var bar = loader.Load("Gate.Tests.StartupTests.Loader.MultiConfigs.Bar");

            MultiConfigs.FooCalls = 0;
            MultiConfigs.BarCalls = 0;

            foo(null);

            Assert.That(MultiConfigs.FooCalls, Is.EqualTo(1));
            Assert.That(MultiConfigs.BarCalls, Is.EqualTo(0));

            bar(null);

            Assert.That(MultiConfigs.FooCalls, Is.EqualTo(1));
            Assert.That(MultiConfigs.BarCalls, Is.EqualTo(1));
        }

        [Test]
        public void Configuration_method_defaults_to_Configuration_if_only_type_name_is_provided()
        {
            var loader = new DefaultConfigurationLoader();
            var configuration = loader.Load("Gate.Tests.StartupTests.Loader.MultiConfigs");

            MultiConfigs.FooCalls = 0;
            MultiConfigs.BarCalls = 0;
            MultiConfigs.ConfigurationCalls = 0;

            Assert.That(MultiConfigs.FooCalls, Is.EqualTo(0));
            Assert.That(MultiConfigs.BarCalls, Is.EqualTo(0));
            Assert.That(MultiConfigs.ConfigurationCalls, Is.EqualTo(0));

            configuration(null);

            Assert.That(MultiConfigs.FooCalls, Is.EqualTo(0));
            Assert.That(MultiConfigs.BarCalls, Is.EqualTo(0));
            Assert.That(MultiConfigs.ConfigurationCalls, Is.EqualTo(1));
        }


        [Test]
        public void Comma_may_be_used_if_assembly_name_doesnt_match_namespace()
        {
            var loader = new DefaultConfigurationLoader();
            var configuration = loader.Load("DifferentNamespace.DoesNotFollowConvention, Gate.Tests");

            DoesNotFollowConvention.ConfigurationCalls = 0;

            configuration(null);

            Assert.That(DoesNotFollowConvention.ConfigurationCalls, Is.EqualTo(1));
        }

        static int _alphaCalls;

        public static AppDelegate Alpha()
        {
            return (env, result, fault) => ++_alphaCalls;
        }

        [Test]
        public void Method_that_returns_app_action_may_also_be_called()
        {
            var loader = new DefaultConfigurationLoader();
            var configuration = loader.Load("Gate.Tests.StartupTests.Loader.DefaultConfigurationLoaderTests.Alpha");

            var app = new AppBuilder(configuration).Build();
            _alphaCalls = 0;
            app(null, null, null);
            Assert.That(_alphaCalls, Is.EqualTo(1));
        }

        [Test]
        public void Startup_Configuration_in_assembly_namespace_will_be_discovered_by_default()
        {
            var loader = new DefaultConfigurationLoader();
            var configuration = loader.Load("");
            Startup.ConfigurationCalls = 0;
            configuration(null);
            Assert.That(Startup.ConfigurationCalls, Is.EqualTo(1));

            configuration = loader.Load(null);
            Startup.ConfigurationCalls = 0;
            configuration(null);
            Assert.That(Startup.ConfigurationCalls, Is.EqualTo(1));
        }
    }

    public class MultiConfigs
    {
        public static int FooCalls;

        public static void Foo(AppBuilder builder)
        {
            FooCalls += 1;
        }

        public static int BarCalls;

        public static void Bar(AppBuilder builder)
        {
            BarCalls += 1;
        }

        public static int ConfigurationCalls;

        public static void Configuration(AppBuilder builder)
        {
            ConfigurationCalls += 1;
        }
    }
}

namespace DifferentNamespace
{
    public class DoesNotFollowConvention
    {
        public static int ConfigurationCalls;

        public static void Configuration(AppBuilder builder)
        {
            ConfigurationCalls += 1;
        }
    }
}