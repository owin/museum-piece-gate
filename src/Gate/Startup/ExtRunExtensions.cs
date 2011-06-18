using System;
using System.Collections.Generic;

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

    public static class ExtRunExtensions
    {
        /* 
         * extension methods to support passing in an already-built delegate
         */

        public static IAppBuilder Run(this AppBuilderExt builder, AppAction app)
        {
            return builder.Run(() => app);
        }


        /* 
         * extension methods take an AppAction factory func and it's associated parameters
         */

        public static IAppBuilder Run<T1>(this AppBuilderExt builder, Func<T1, AppAction> factory, T1 arg1)
        {
            return builder.Run(() => factory(arg1));
        }

        public static IAppBuilder Run<T1, T2>(this AppBuilderExt builder, Func<T1, T2, AppAction> factory, T1 arg1, T2 arg2)
        {
            return builder.Run(() => factory(arg1, arg2));
        }

        public static IAppBuilder Run<T1, T2, T3>(this AppBuilderExt builder, Func<T1, T2, T3, AppAction> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Run(() => factory(arg1, arg2, arg3));
        }

        public static IAppBuilder Run<T1, T2, T3, T4>(this AppBuilderExt builder, Func<T1, T2, T3, T4, AppAction> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Run(() => factory(arg1, arg2, arg3, arg4));
        }
    }
}
