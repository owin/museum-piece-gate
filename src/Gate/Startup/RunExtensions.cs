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

    public static class RunExtensions
    {
        /* 
         * extension methods to support passing in an already-built delegate
         */

        public static AppBuilder Run(this AppBuilder builder, AppDelegate app)
        {
            return builder.Run(() => app);
        }


        /* 
         * extension methods take an AppDelegate factory func and it's associated parameters
         */

        public static AppBuilder Run<T1>(this AppBuilder builder, Func<T1, AppDelegate> factory, T1 arg1)
        {
            return builder.Run(() => factory(arg1));
        }

        public static AppBuilder Run<T1, T2>(this AppBuilder builder, Func<T1, T2, AppDelegate> factory, T1 arg1, T2 arg2)
        {
            return builder.Run(() => factory(arg1, arg2));
        }

        public static AppBuilder Run<T1, T2, T3>(this AppBuilder builder, Func<T1, T2, T3, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Run(() => factory(arg1, arg2, arg3));
        }

        public static AppBuilder Run<T1, T2, T3, T4>(this AppBuilder builder, Func<T1, T2, T3, T4, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Run(() => factory(arg1, arg2, arg3, arg4));
        }


        /* 
         * extension methods take an IApplication type and it's associated parameters
         */

        public static AppBuilder Run<TApplication>(this AppBuilder builder) where TApplication : IApplication, new()
        {
            return builder.Run(new TApplication().Create);
        }

        public static AppBuilder Run<TApplication, T1>(this AppBuilder builder, T1 arg1) where TApplication : IApplication<T1>, new()
        {
            return builder.Run(() => new TApplication().Create(arg1));
        }

        public static AppBuilder Run<TApplication, T1, T2>(this AppBuilder builder, T1 arg1, T2 arg2) where TApplication : IApplication<T1, T2>, new()
        {
            return builder.Run(() => new TApplication().Create(arg1, arg2));
        }

        public static AppBuilder Run<TApplication, T1, T2, T3>(this AppBuilder builder, T1 arg1, T2 arg2, T3 arg3) where TApplication : IApplication<T1, T2, T3>, new()
        {
            return builder.Run(() => new TApplication().Create(arg1, arg2, arg3));
        }

        public static AppBuilder Run<TApplication, T1, T2, T3, T4>(this AppBuilder builder, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where TApplication : IApplication<T1, T2, T3, T4>, new()
        {
            return builder.Run(() => new TApplication().Create(arg1, arg2, arg3, arg4));
        }

    }
}
