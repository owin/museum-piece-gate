using System;
using System.Collections.Generic;
using Gate.Owin;

namespace Gate
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

    public static class AppBuilderRunExtensions
    {
        /*
         * Fundamental definition of Run.
         */

        public static IAppBuilder Run(this IAppBuilder builder, Func<AppDelegate> appFactory)
        {
            return builder.Use(_ => appFactory());
        }

        /* 
         * Extension method to support passing in an already-built delegate.
         */

        public static IAppBuilder Run(this IAppBuilder builder, AppDelegate app)
        {
            return builder.Run(() => app);
        }

        /* 
         * Extension methods take an AppDelegate factory func and its associated parameters.
         */

        public static IAppBuilder Run<T1>(this IAppBuilder builder, Func<T1, AppDelegate> factory, T1 arg1)
        {
            return builder.Run(() => factory(arg1));
        }

        public static IAppBuilder Run<T1, T2>(this IAppBuilder builder, Func<T1, T2, AppDelegate> factory, T1 arg1, T2 arg2)
        {
            return builder.Run(() => factory(arg1, arg2));
        }

        public static IAppBuilder Run<T1, T2, T3>(this IAppBuilder builder, Func<T1, T2, T3, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Run(() => factory(arg1, arg2, arg3));
        }

        public static IAppBuilder Run<T1, T2, T3, T4>(this IAppBuilder builder, Func<T1, T2, T3, T4, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Run(() => factory(arg1, arg2, arg3, arg4));
        }

        /* 
         * Extension method to support passing in an already-built action.
         */

        public static IAppBuilder Run(this IAppBuilder builder, AppAction app)
        {
            return builder.Run(() => app.ToDelegate());
        }


    }
}
