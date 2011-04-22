using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    
    public delegate void AppDelegate(
        IDictionary<string, object> env,
        ResultDelegate result,
        Action<Exception> fault);

    public delegate void ResultDelegate(
        string status,
        IDictionary<string, string> headers,
        BodyDelegate body);

    public delegate Action /* cancel */ BodyDelegate(
        Func<
            ArraySegment<byte>, // data
            Action, // continuation
            bool> // continuation was or will be invoked
            onNext,
        Action<Exception> onError,
        Action onComplete);
    
    

    public static class Delegates
    {
        public static AppAction ToAction(this AppDelegate method)
        {
            return
                (env, result, fault) =>
                method(
                    env,
                    (status, headers, body) =>
                    result(
                        status,
                        headers,
                        (next, error, complete) =>
                        body(next, error, complete)),
                    fault);
        }

        public static AppDelegate ToDelegate(this AppAction method) {
            return
                (env, result, fault) =>
                method(
                    env,
                    (status, headers, body) =>
                    result(
                        status,
                        headers,
                        (next, error, complete) =>
                        body(next, error, complete)),
                    fault);
        }
    }
}
