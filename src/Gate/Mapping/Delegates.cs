using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gate.Owin;

namespace Gate
{
    using ResultTuple = Tuple<string, IDictionary<String, String>, BodyDelegate>;

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

    using BodyAction = Func< //body
        Func< //next
            ArraySegment<byte>, // data
            Action, // continuation
            bool>, // continuation was or will be invoked
        Action<Exception>, //error
        Action, //complete
        Action>; //cancel


    public static class Delegates
    {
        public static AppAction ToAction(this AppDelegate app)
        {
            return
                (env, result, fault) =>
                app(
                    env,
                    (status, headers, body) =>
                    result(
                        status,
                        headers,
                        (next, error, complete) =>
                        body(next, error, complete)),
                    fault);
        }

        public static AppDelegate ToDelegate(this AppAction app)
        {
            return
                (env, result, fault) =>
                app(
                    env,
                    (status, headers, body) =>
                    result(
                        status,
                        headers,
                        (next, error, complete) =>
                        body(next, error, complete)),
                    fault);
        }

        public static BodyAction ToAction(this BodyDelegate body)
        {
            return (next, error, complete) => body(next, error, complete);
        }

        public static BodyDelegate ToDelegate(this BodyAction body)
        {
            return (next, error, complete) => body(next, error, complete);
        }


        public static AppTaskDelegate ToTaskDelegate(this AppDelegate app)
        {
            return
                env =>
                {
                    var tcs = new TaskCompletionSource<ResultTuple>();
                    app(
                        env,
                        (status, headers, body) => tcs.SetResult(Tuple.Create(status, headers, body)),
                        tcs.SetException);
                    return tcs.Task;
                };
        }

        public static AppDelegate ToDelegate(this AppTaskDelegate app)
        {
            return
                (env, result, fault) =>
                {
                    var task = app(env);
                    task.ContinueWith(
                        t => result(t.Result.Item1, t.Result.Item2, t.Result.Item3),
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
                    task.ContinueWith(
                        t => fault(t.Exception),
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
                };
        }
    }
}
