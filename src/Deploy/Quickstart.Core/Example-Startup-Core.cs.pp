using System;
using System.Linq;
using System.Reflection;
using Gate.Middleware;
using Owin;

namespace $rootnamespace$
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            foreach (var pipeline in FindMembers("Pipeline"))
            {
                pipeline(builder);
            }
            builder.RunCascade(FindMembers("Cascade"));
        }

        public void Debug(IAppBuilder builder)
        {
            builder.UseShowExceptions();
            Configuration(builder);
        }

        Action<IAppBuilder>[] FindMembers(string prefix)
        {
            return GetType().GetMethods()
                .Where(mi => mi.Name.StartsWith(prefix))
                .OrderBy(mi => mi.Name, StringComparer.OrdinalIgnoreCase)
                .Select<MethodInfo, Action<IAppBuilder>>(mi => (arg0 => mi.Invoke(this, new[] { arg0 })))
                .ToArray();
        }
    }
}
