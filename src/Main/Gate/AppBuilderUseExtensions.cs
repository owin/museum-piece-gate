using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Owin;

namespace Gate
{
    using AppAction = Func< // Call
        IDictionary<string, object>, // Environment
        IDictionary<string, string[]>, // Headers
        Stream, // Body
        Task<Tuple< //Result
            IDictionary<string, object>, // Properties
            int, // Status
            IDictionary<string, string[]>, // Headers
            Func< // CopyTo
                Stream, // Body
                Task>>>>; // Done

    public static class AppBuilderUseExtensions
    {
        /* 
         * Extension methods take an AppDelegate factory func and its associated parameters.
         */

        public static IAppBuilder Use<TApp, T1>(this IAppBuilder builder, Func<TApp, T1, TApp> middleware, T1 arg1)
        {
            return builder.Use<TApp>(app => middleware(app, arg1));
        }

        public static IAppBuilder Use<TApp, T1, T2>(this IAppBuilder builder, Func<TApp, T1, T2, TApp> middleware, T1 arg1, T2 arg2)
        {
            return builder.Use<TApp>(app => middleware(app, arg1, arg2));
        }

        public static IAppBuilder Use<TApp, T1, T2, T3>(this IAppBuilder builder, Func<TApp, T1, T2, T3, TApp> middleware, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Use<TApp>(app => middleware(app, arg1, arg2, arg3));
        }

        public static IAppBuilder Use<TApp, T1, T2, T3, T4>(this IAppBuilder builder, Func<TApp, T1, T2, T3, T4, TApp> middleware, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Use<TApp>(app => middleware(app, arg1, arg2, arg3, arg4));
        }



        // strongly-typed to avoid "method group" issues when a class method is used
        public static IAppBuilder Use(this IAppBuilder builder, Func<AppDelegate, AppDelegate> middleware)
        { return builder.Use<AppDelegate>(middleware); }

        public static IAppBuilder Use<T1>(this IAppBuilder builder, Func<AppDelegate, T1, AppDelegate> middleware, T1 arg1)
        { return builder.Use<AppDelegate>(app => middleware(app, arg1)); }

        public static IAppBuilder Use<T1, T2>(this IAppBuilder builder, Func<AppDelegate, T1, T2, AppDelegate> middleware, T1 arg1, T2 arg2)
        { return builder.Use<AppDelegate>(app => middleware(app, arg1, arg2)); }

        public static IAppBuilder Use<T1, T2, T3>(this IAppBuilder builder, Func<AppDelegate, T1, T2, T3, AppDelegate> middleware, T1 arg1, T2 arg2, T3 arg3)
        { return builder.Use<AppDelegate>(app => middleware(app, arg1, arg2, arg3)); }

        public static IAppBuilder Use<T1, T2, T3, T4>(this IAppBuilder builder, Func<AppDelegate, T1, T2, T3, T4, AppDelegate> middleware, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        { return builder.Use<AppDelegate>(app => middleware(app, arg1, arg2, arg3, arg4)); }
    }
}