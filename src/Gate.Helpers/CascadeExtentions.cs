using System;
using System.Linq;
using Gate.Startup;

namespace Gate.Helpers
{
    public static class CascadeExtentions 
    {
        public static AppBuilder Cascade(this AppBuilder builder, params AppDelegate[] apps)
        {
            return builder.Run(Helpers.Cascade.Create(apps));
        }
        public static AppBuilder Cascade(this AppBuilder builder, params Action<AppBuilder>[] apps)
        {
            return builder.Run(Helpers.Cascade.Create(apps.Select(builder.Branch)));
        }
    }
}