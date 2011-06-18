﻿using System;
using System.Collections.Generic;

namespace Gate
{
    public static class MapExtensions
    {
        /*
         * Fundamental definition of Map.
         */

        public static IAppBuilder Map(this IAppBuilder builder, string path, AppDelegate app)
        {
            var mapBuilder = builder as MapBuilder ?? new MapBuilder(builder, UrlMapper.Create);
            mapBuilder.MapInternal(path, app);
            return mapBuilder;
        }

        /*
         * Extension to allow branching of AppBuilder.
         */

        public static IAppBuilder Map(this IAppBuilder builder, string path, Action<IAppBuilder> app)
        {
            return builder.Map(path, AppBuilder.BuildConfiguration(app));
        }

        /*
         * Extensions to map AppDelegate factory func to a given path, with optional parameters.
         */

        public static IAppBuilder Map(this IAppBuilder builder, string path, Func<AppDelegate> factory)
        {
            return builder.Map(path, b2 => b2.Run(factory));
        }

        public static IAppBuilder Map<T1>(this IAppBuilder builder, string path, Func<T1, AppDelegate> factory, T1 arg1)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1));
        }

        public static IAppBuilder Map<T1, T2>(this IAppBuilder builder, string path, Func<T1, T2, AppDelegate> factory, T1 arg1, T2 arg2)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2));
        }

        public static IAppBuilder Map<T1, T2, T3>(this IAppBuilder builder, string path, Func<T1, T2, T3, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2, arg3));
        }

        public static IAppBuilder Map<T1, T2, T3, T4>(this IAppBuilder builder, string path, Func<T1, T2, T3, T4, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2, arg3, arg4));
        }
    }
}