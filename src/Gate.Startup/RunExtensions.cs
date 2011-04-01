using System;
using System.Collections.Generic;

namespace Gate.Startup
{
    using AppDelegate = Action< // app
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
        public static AppBuilder Run(this AppBuilder builder, AppDelegate app)
        {
            return builder.Run(() => app);
        }


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

        public static AppBuilder Use<T1>(this AppBuilder builder, Func<AppDelegate, T1, AppDelegate> factory, T1 arg1)
        {
            return builder.Use(app => factory(app, arg1));
        }

        public static AppBuilder Use<T1, T2>(this AppBuilder builder, Func<AppDelegate, T1, T2, AppDelegate> factory, T1 arg1, T2 arg2)
        {
            return builder.Use(app => factory(app, arg1, arg2));
        }

        public static AppBuilder Use<T1, T2, T3>(this AppBuilder builder, Func<AppDelegate, T1, T2, T3, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Use(app => factory(app, arg1, arg2, arg3));
        }

        public static AppBuilder Use<T1, T2, T3, T4>(this AppBuilder builder, Func<AppDelegate, T1, T2, T3, T4, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Use(app => factory(app, arg1, arg2, arg3, arg4));
        }
    }
}