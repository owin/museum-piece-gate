﻿using System;
using System.Linq;
using Gate.Builder;
using Gate.Owin;

namespace Gate.Middleware
{
    public static class CascadeExtentions
    {
        public static IAppBuilder Cascade(this IAppBuilder builder, params AppDelegate[] apps)
        {
            return builder.Run(Middleware.Cascade.Middleware(apps));
        }

        public static IAppBuilder Cascade(this IAppBuilder builder, params Action<IAppBuilder>[] apps)
        {
            return builder.Cascade(apps.Select(builder.Build).ToArray());
        }
    }
}