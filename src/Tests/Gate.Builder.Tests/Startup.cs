
using Gate.Owin;

namespace Gate.Builder.Tests
{
    public class Startup
    {
        public static void Configuration(IAppBuilder builder)
        {
            ++ConfigurationCalls;
        }

        public static int ConfigurationCalls { get; set; }
    }
}