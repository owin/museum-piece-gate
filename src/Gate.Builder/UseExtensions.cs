using System;
using System.Collections.Generic;

namespace Gate.Builder
{
    using AppAction = Action< // app
         IDictionary<string, object>, // env
         Action< // result
             string, // status
             IDictionary<string, string>, // headers
             Func< // body
                 Func< // next
                     ArraySegment<byte>, // data
                     Action, // continuation
                     bool>, // async                    
                 Action<Exception>, // error
                 Action, // complete
                 Action>>, // cancel
         Action<Exception>>; // error

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
         * Extension methods take an AppAction factory func and its associated parameters.
         */

        public static IAppBuilder Use(this IAppBuilder builder, Func<AppAction, AppAction> factory)
        {
            return builder.Use(app => factory(app.ToAction()).ToDelegate());
        }

        public static IAppBuilder Use<T1>(this IAppBuilder builder, Func<AppAction, T1, AppAction> factory, T1 arg1)
        {
            return builder.Use(app => factory(app.ToAction(), arg1).ToDelegate());
        }

        public static IAppBuilder Use<T1, T2>(this IAppBuilder builder, Func<AppAction, T1, T2, AppAction> factory, T1 arg1, T2 arg2)
        {
            return builder.Use(app => factory(app.ToAction(), arg1, arg2).ToDelegate());
        }

        public static IAppBuilder Use<T1, T2, T3>(this IAppBuilder builder, Func<AppAction, T1, T2, T3, AppAction> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Use(app => factory(app.ToAction(), arg1, arg2, arg3).ToDelegate());
        }

        public static IAppBuilder Use<T1, T2, T3, T4>(this IAppBuilder builder, Func<AppAction, T1, T2, T3, T4, AppAction> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Use(app => factory(app.ToAction(), arg1, arg2, arg3, arg4).ToDelegate());
        }
    }
}