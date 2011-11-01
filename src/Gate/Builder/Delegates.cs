﻿using System;
using System.Collections.Generic;
using Gate.Owin;

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

        public static AppDelegate ToDelegate(this AppAction app) {
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

        public static BodyAction ToAction(this BodyDelegate body) {
            return (next, error, complete) => body(next, error, complete);
        }

        public static BodyDelegate ToDelegate(this BodyAction body) {
            return (next, error, complete) => body(next, error, complete);
        }
    }
}
