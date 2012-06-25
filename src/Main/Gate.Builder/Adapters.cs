using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Owin;

namespace Gate.Builder
{
    //    using ResultTuple = Tuple<string, IDictionary<String, string[]>, BodyDelegate>;

    //#pragma warning disable 811
    //    using AppAction = Action< // app
    //        IDictionary<string, object>, // env
    //        Action< // result
    //            string, // status
    //            IDictionary<string, string[]>, // headers
    //            Action< // body
    //                Func< // write
    //                    ArraySegment<byte>, // data                     
    //                    Action, // continuation
    //                    bool>, // buffering
    //                Action< // end
    //                    Exception>, // error
    //                CancellationToken>>, // cancel
    //        Action<Exception>>; // error

    //    using BodyAction = Action< // body
    //        Func< // write
    //            ArraySegment<byte>, // data                     
    //            Action, // continuation
    //            bool>, // buffering
    //        Action< // end
    //            Exception>, // error
    //        CancellationToken>; //cancel


    public static class Adapters
    {
        //public static AppAction ToAction(AppDelegate app)
        //{
        //    return
        //        (env, result, fault) =>
        //        {
        //            var revert = Replace<BodyAction, BodyDelegate>(env, ToDelegate);
        //            app(
        //                env,
        //                (status, headers, body) =>
        //                {
        //                    revert();
        //                    result(status, headers, ToAction(body));
        //                },
        //                ex =>
        //                {
        //                    revert();
        //                    fault(ex);
        //                });
        //        };
        //}

        //public static AppDelegate ToDelegate(AppAction app)
        //{
        //    return
        //        (env, result, fault) =>
        //        {
        //            var revert = Replace<BodyDelegate, BodyAction>(env, ToAction);
        //            app(
        //                env,
        //                (status, headers, body) =>
        //                {
        //                    revert();
        //                    result(status, headers, ToDelegate(body));
        //                },
        //                ex =>
        //                {
        //                    revert();
        //                    fault(ex);
        //                });
        //        };
        //}

        //static Action Replace<TFrom, TTo>(IDictionary<string, object> env, Func<TFrom, TTo> adapt)
        //{
        //    object body;
        //    if (env.TryGetValue(OwinConstants.RequestBody, out body) && body is TFrom)
        //    {
        //        env[OwinConstants.RequestBody] = adapt((TFrom)body);
        //        return () => env[OwinConstants.RequestBody] = body;
        //    }
        //    return () => { };
        //}

        //public static BodyAction ToAction(BodyDelegate body)
        //{
        //    return (write, end, cancel) => body(write, end, cancel);
        //}

        //public static BodyDelegate ToDelegate(BodyAction body)
        //{
        //    return (write, end, cancel) => body(write, end, cancel);
        //}


        public static AppTaskDelegate ToTaskDelegate(AppDelegate app)
        {
            return
                call =>
                {
                    var tcs = new TaskCompletionSource<ResultParameters>();
                    app(
                        call,
                        (result, exception) =>
                        {
                            if (exception == null)
                            {
                                tcs.SetResult(result);
                            }
                            else
                            {
                                tcs.SetException(exception);
                            }
                        });
                    return tcs.Task;
                };
        }

        public static AppDelegate ToDelegate(AppTaskDelegate app)
        {
            return
                (call, callback) =>
                    app(call)
                        .Then(result => callback(result, null))
                        .Catch(caught =>
                        {
                            callback(default(ResultParameters), caught.Exception);
                            return caught.Handled();
                        });
        }
    }
}
