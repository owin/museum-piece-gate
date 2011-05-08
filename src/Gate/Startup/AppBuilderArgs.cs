﻿using System;

namespace Gate.Startup
{
    public static class AppBuilderArgs
    {
        public static AppBuilderArgs<T1> WithArgs<T1>(this AppBuilder builder, T1 arg1)
        {
            return new AppBuilderArgs<T1>(builder, Tuple.Create(arg1));
        }

        public static AppBuilderArgs<T1, T2> WithArgs<T1, T2>(this AppBuilder builder, T1 arg1, T2 arg2)
        {
            return new AppBuilderArgs<T1, T2>(builder, Tuple.Create(arg1, arg2));
        }

        public static AppBuilderArgs<T1, T2, T3> WithArgs<T1, T2, T3>(this AppBuilder builder, T1 arg1, T2 arg2, T3 arg3)
        {
            return new AppBuilderArgs<T1, T2, T3>(builder, Tuple.Create(arg1, arg2, arg3));
        }

        public static AppBuilderArgs<T1, T2, T3, T4> WithArgs<T1, T2, T3, T4>(this AppBuilder builder, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return new AppBuilderArgs<T1, T2, T3, T4>(builder, Tuple.Create(arg1, arg2, arg3, arg4));
        }
    }

    public class AppBuilderArgs<T1>
    {
        public AppBuilderArgs(AppBuilder builder, Tuple<T1> args)
        {
            Builder = builder;
            Args = args;
        }

        public AppBuilder Builder { get; set; }
        public Tuple<T1> Args { get; private set; }

        public AppBuilder Run<TApp>() where TApp : IApplication<T1>, new()
        {
            return Builder.Run(() => new TApp().Create(Args.Item1));
        }

        public AppBuilder Use<TMiddleware>() where TMiddleware : IMiddleware<T1>, new()
        {
            return Builder.Use(app => new TMiddleware().Create(app, Args.Item1));
        }
    }

    public class AppBuilderArgs<T1, T2>
    {
        public AppBuilderArgs(AppBuilder builder, Tuple<T1, T2> args)
        {
            Builder = builder;
            Args = args;
        }

        public AppBuilder Builder { get; set; }
        public Tuple<T1, T2> Args { get; private set; }

        public AppBuilder Run<TApp>() where TApp : IApplication<T1, T2>, new()
        {
            return Builder.Run(() => new TApp().Create(Args.Item1, Args.Item2));
        }

        public AppBuilder Use<TMiddleware>() where TMiddleware : IMiddleware<T1, T2>, new()
        {
            return Builder.Use(app => new TMiddleware().Create(app, Args.Item1, Args.Item2));
        }
    }

    public class AppBuilderArgs<T1, T2, T3>
    {
        public AppBuilderArgs(AppBuilder builder, Tuple<T1, T2, T3> args)
        {
            Builder = builder;
            Args = args;
        }

        public AppBuilder Builder { get; set; }
        public Tuple<T1, T2, T3> Args { get; private set; }

        public AppBuilder Run<TApp>() where TApp : IApplication<T1, T2, T3>, new()
        {
            return Builder.Run(() => new TApp().Create(Args.Item1, Args.Item2, Args.Item3));
        }

        public AppBuilder Use<TMiddleware>() where TMiddleware : IMiddleware<T1, T2, T3>, new()
        {
            return Builder.Use(app => new TMiddleware().Create(app, Args.Item1, Args.Item2, Args.Item3));
        }
    }

    public class AppBuilderArgs<T1, T2, T3, T4>
    {
        public AppBuilderArgs(AppBuilder builder, Tuple<T1, T2, T3, T4> args)
        {
            Builder = builder;
            Args = args;
        }

        public AppBuilder Builder { get; set; }
        public Tuple<T1, T2, T3, T4> Args { get; private set; }

        public AppBuilder Run<TApp>() where TApp : IApplication<T1, T2, T3, T4>, new()
        {
            return Builder.Run(() => new TApp().Create(Args.Item1, Args.Item2, Args.Item3, Args.Item4));
        }

        public AppBuilder Use<TMiddleware>() where TMiddleware : IMiddleware<T1, T2, T3, T4>, new()
        {
            return Builder.Use(app => new TMiddleware().Create(app, Args.Item1, Args.Item2, Args.Item3, Args.Item4));
        }
    }
}