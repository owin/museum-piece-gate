using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gate.Middleware;
using System.Threading.Tasks;
using System.IO;

namespace Owin
{
    public static class HeadSuppressionExtensions
    {
        public static IAppBuilder UseHeadSuppression(this IAppBuilder builder)
        {
            return builder.UseType<HeadSuppressionMiddleware>();
        }
    }
}

namespace Gate.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    // This middleware can be used to suppress output incorrectly written by other middleware or applications for HEAD requests.
    public class HeadSuppressionMiddleware
    {
        private AppFunc nextApp;

        public HeadSuppressionMiddleware(AppFunc nextApp)
        {
            this.nextApp = nextApp;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            if ("HEAD".Equals(env.Get<string>(OwinConstants.RequestMethod), StringComparison.OrdinalIgnoreCase))
            {
                env[OwinConstants.ResponseBody] = Stream.Null;
            }

            return nextApp(env);
        }
    }
}
