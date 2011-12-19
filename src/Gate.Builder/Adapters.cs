using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gate.Owin;

namespace Gate.Builder
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


    public static class Adapters
    {
        public static AppAction ToAction(AppDelegate app)
        {
            return
                (env, result, fault) =>
                {
                    var revert = Replace<BodyAction, BodyDelegate>(env, ToDelegate);
                    app(
                        env,
                        (status, headers, body) =>
                        {
                            revert();
                            result(status, headers, ToAction(body));
                        },
                        ex =>
                        {
                            revert();
                            fault(ex);
                        });
                };
        }

        public static AppDelegate ToDelegate(AppAction app)
        {
            return
                (env, result, fault) =>
                {
                    var revert = Replace<BodyDelegate, BodyAction>(env, ToAction);
                    app(
                        env,
                        (status, headers, body) =>
                        {
                            revert();
                            result(status, headers, ToDelegate(body));
                        },
                        ex =>
                        {
                            revert();
                            fault(ex);
                        });
                };
        }

        static Action Replace<TFrom, TTo>(IDictionary<string, object> env, Func<TFrom, TTo> adapt)
        {
            object body;
            if (env.TryGetValue("owin.RequestBody", out body) && body is TFrom)
            {
                env["owin.RequestBody"] = adapt((TFrom)body);
                return () => env["owin.RequestBody"] = body;
            }
            return () => { };
        }

        public static BodyAction ToAction(BodyDelegate body)
        {
            return (next, error, complete) => body(next, error, complete);
        }

        public static BodyDelegate ToDelegate(BodyAction body)
        {
            return (next, error, complete) => body(next, error, complete);
        }


        public static AppTaskDelegate ToTaskDelegate(AppDelegate app)
        {
            return
                env =>
                {
                    var tcs = new TaskCompletionSource<ResultTuple>();
                    app(
                        env,
                        (status, headers, body) => tcs.SetResult(new ResultTuple(status, headers, body)),
                        tcs.SetException);
                    return tcs.Task;
                };
        }

        public static AppDelegate ToDelegate(AppTaskDelegate app)
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
