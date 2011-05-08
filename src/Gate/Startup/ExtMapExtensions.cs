using System;
using System.Collections.Generic;

namespace Gate.Startup
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


    public static class ExtMapExtensions
    {
        public static AppBuilder Map(this AppBuilderExt builder, string path, AppAction app)
        {
            return builder.Map(path, b2 => b2.Run(app));
        }

        public static AppBuilder Map(this AppBuilderExt builder, string path, Func<AppAction> factory)
        {
            return builder.Map(path, b2 => b2.Run(factory));
        }

        public static AppBuilder Map<T1>(this AppBuilderExt builder, string path, Func<T1, AppAction> factory, T1 arg1)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1));
        }

        public static AppBuilder Map<T1, T2>(this AppBuilderExt builder, string path, Func<T1, T2, AppAction> factory, T1 arg1, T2 arg2)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2));
        }

        public static AppBuilder Map<T1, T2, T3>(this AppBuilderExt builder, string path, Func<T1, T2, T3, AppAction> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2, arg3));
        }

        public static AppBuilder Map<T1, T2, T3, T4>(this AppBuilderExt builder, string path, Func<T1, T2, T3, T4, AppAction> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2, arg3, arg4));
        }
    }
}