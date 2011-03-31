using System.Linq;
using Gate.Startup.Loader;
using NUnit.Framework;

namespace Gate.Startup.Tests.Loader
{
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
            var configuration = loader.Load("Gate.Startup.Tests.Loader.DefaultConfigurationLoaderTests.Hello");

            _helloCalls = 0;
            configuration(null);
            Assert.That(_helloCalls, Is.EqualTo(1));
        }

        [Test]
        public void An_extra_segment_will_cause_the_match_to_fail()
        {
            var loader = new DefaultConfigurationLoader();
            var configuration = loader.Load("Gate.Startup.Tests.Loader.DefaultConfigurationLoaderTests.Hello.Bar");

            Assert.That(configuration, Is.Null);
        }

        [Test]
        public void Calling_a_class_with_multiple_configs_is_okay()
        {
            var loader = new DefaultConfigurationLoader();
            var foo = loader.Load("Gate.Startup.Tests.Loader.MultiConfigs.Foo");
            var bar = loader.Load("Gate.Startup.Tests.Loader.MultiConfigs.Bar");

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
            var configuration = loader.Load("Gate.Startup.Tests.Loader.MultiConfigs");

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
    }

    internal class MultiConfigs
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