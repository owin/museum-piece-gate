using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate
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

    public interface IMiddleware
    {
        AppDelegate Create(AppDelegate app);
    }

    public interface IMiddleware<in T1>
    {
        AppDelegate Create(AppDelegate app, T1 t1);
    }

    public interface IMiddleware<in T1, in T2>
    {
        AppDelegate Create(AppDelegate app, T1 arg1, T2 arg2);
    }
    
    public interface IMiddleware<in T1, in T2, in T3>
    {
        AppDelegate Create(AppDelegate app, T1 arg1, T2 arg2, T3 arg3);
    }

    public interface IMiddleware<in T1, in T2, in T3, in T4>
    {
        AppDelegate Create(AppDelegate app, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    }
}
