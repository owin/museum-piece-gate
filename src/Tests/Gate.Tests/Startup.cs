
using Gate.Builder;
using Gate.Owin;

namespace Gate.Tests
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