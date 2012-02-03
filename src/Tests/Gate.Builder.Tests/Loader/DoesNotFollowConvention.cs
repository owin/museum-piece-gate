using Gate.Owin;

namespace DifferentNamespace
{
    public class DoesNotFollowConvention
    {
        public static int ConfigurationCalls;

        public static void Configuration(IAppBuilder builder)
        {
            ConfigurationCalls += 1;
        }
    }
}