using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Middleware
{
    public static class MethodOverrideExtensions
    {
        public static IAppBuilder MethodOverride(this IAppBuilder builder)
        {
            return builder.Transform((e, c, ex) =>
            {
                if (e.Headers.ContainsKey("x-http-method-override"))
                    e.Method = e.Headers["x-http-method-override"];

                c(e);
            });
        }
    }
}
