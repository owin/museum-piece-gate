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

    public static class MapExtensions
    {
        public static AppBuilder SetUrlMapper(this AppBuilder builder, Func<IDictionary<string, AppDelegate>, AppDelegate> mapper)
        {
            return builder.SetUrlMapper((_, maps) => mapper(maps));
        }

        public static AppBuilder Map(this AppBuilder builder, string path, AppDelegate app)
        {
            return builder.Map(path, b2 => b2.Run(app));
        }

        public static AppBuilder Map(this AppBuilder builder, string path, Func<AppDelegate> factory)
        {
            return builder.Map(path, b2 => b2.Run(factory));
        }

        public static AppBuilder Map<T1>(this AppBuilder builder, string path, Func<T1, AppDelegate> factory, T1 arg1)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1));
        }

        public static AppBuilder Map<T1, T2>(this AppBuilder builder, string path,Func<T1, T2, AppDelegate> factory, T1 arg1, T2 arg2)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2));
        }

        public static AppBuilder Map<T1, T2, T3>(this AppBuilder builder, string path,Func<T1, T2, T3, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2, arg3));
        }

        public static AppBuilder Map<T1, T2, T3, T4>(this AppBuilder builder, string path,Func<T1, T2, T3, T4, AppDelegate> factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.Map(path, b2 => b2.Run(factory, arg1, arg2, arg3, arg4));
        }

    }
}