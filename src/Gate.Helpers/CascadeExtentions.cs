using System;
using System.Linq;

namespace Gate.Helpers
{
    public static class CascadeExtentions
    {
        public static IAppBuilder Cascade(this IAppBuilder builder, params AppDelegate[] apps)
        {
            return builder.Run(Helpers.Cascade.Create(apps));
        }

        public static IAppBuilder Cascade(this IAppBuilder builder, params Action<IAppBuilder>[] apps)
        {
            return builder.Cascade(apps.Select(config => AppBuilder.BuildConfiguration(config)).ToArray());
        }
    }
}