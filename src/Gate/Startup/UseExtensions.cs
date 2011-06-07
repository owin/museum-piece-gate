using System;
using System.Collections.Generic;

namespace Gate.Startup
{
    public static class UseExtensions
    {
        /* 
         * Extension methods take an AppDelegate factory func and its associated parameters.
         */

        public static IAppBuilder Use<T1>(this IAppBuilder builder, Func<AppDelegate, T1, AppDelegate> factory, T1 arg1)
        {
            return builder.Use(app => factory(app, arg1));
        }

        public static IAppBuilder Use<T1, T2>(this IAppBuilder builder, Func<AppDelegate, T1, T2, AppDelegate> factory, T1 arg1, T2 arg2)
        {
            return builder.Use(app => factory(app, arg1, arg2));
        }

        public static IAppBuilder Use<T1, T2, T3>(this IAppBuilder builder, Func<AppDelegate, T1, T2, T3, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Use(app => factory(app, arg1, arg2, arg3));
        }

        public static IAppBuilder Use<T1, T2, T3, T4>(this IAppBuilder builder, Func<AppDelegate, T1, T2, T3, T4, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Use(app => factory(app, arg1, arg2, arg3, arg4));
        }

        /* 
         * extension methods take a type implemeting IMiddleware and it's associated parameters
         */

        public static IAppBuilder Use<TMiddleware>(this IAppBuilder builder) where TMiddleware : IMiddleware, new()
        {
            return builder.Use(new TMiddleware().Create);
        }

        public static IAppBuilder Use<TMiddleware, T1>(this IAppBuilder builder, T1 arg1) where TMiddleware : IMiddleware<T1>, new()
        {
            return builder.Use(app => new TMiddleware().Create(app, arg1));
        }

        public static IAppBuilder Use<TMiddleware, T1, T2>(this IAppBuilder builder, T1 arg1, T2 arg2) where TMiddleware : IMiddleware<T1, T2>, new()
        {
            return builder.Use(app => new TMiddleware().Create(app, arg1, arg2));
        }

        public static IAppBuilder Use<TMiddleware, T1, T2, T3>(this IAppBuilder builder, T1 arg1, T2 arg2, T3 arg3) where TMiddleware : IMiddleware<T1, T2, T3>, new()
        {
            return builder.Use(app => new TMiddleware().Create(app, arg1, arg2, arg3));
        }

        public static IAppBuilder Use<TMiddleware, T1, T2, T3, T4>(this IAppBuilder builder, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where TMiddleware : IMiddleware<T1, T2, T3, T4>, new()
        {
            return builder.Use(app => new TMiddleware().Create(app, arg1, arg2, arg3, arg4));
        }
    }
}