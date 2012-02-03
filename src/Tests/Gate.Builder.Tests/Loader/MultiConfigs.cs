using Gate.Owin;

namespace Gate.Builder.Tests.Loader
{
    public class MultiConfigs
    {
        public static int FooCalls;

        public static void Foo(IAppBuilder builder)
        {
            FooCalls += 1;
        }

        public static int BarCalls;

        public static void Bar(IAppBuilder builder)
        {
            BarCalls += 1;
        }

        public static int ConfigurationCalls;

        public static void Configuration(IAppBuilder builder)
        {
            ConfigurationCalls += 1;
        }
    }
}