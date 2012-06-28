using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Owin;

namespace Gate.Builder
{
    using AppFunc = Func< // Call
        IDictionary<string, object>, // Environment
        IDictionary<string, string[]>, // Headers
        Stream, // Body
        CancellationToken, // CallCancelled
        Task<Tuple< //Result
            IDictionary<string, object>, // Properties
            int, // Status
            IDictionary<string, string[]>, // Headers
            Func< // CopyTo
                Stream, // Body
                CancellationToken, // CopyToCancelled
                Task>>>>; // Done

    using BodyFunc = Func< // CopyTo
        Stream, // Body
        CancellationToken, // CopyToCancelled
        Task>; // Done

    public static class Adapters
    {
        public static AppFunc ToFunc(AppDelegate app)
        {
            return (env, headers, body, cancel) =>
            {
                var task = app(new CallParameters
                {
                    Environment = env,
                    Headers = headers,
                    Body = body
                }, cancel);

                return task.Then(result => Tuple.Create(
                    result.Properties,
                    result.Status,
                    result.Headers,
                    ToFunc(result.Body)));
            };
        }

        public static BodyFunc ToFunc(BodyDelegate body)
        {
            return (stream, cancel) => body(stream, cancel);
        }

        public static AppDelegate ToDelegate(AppFunc app)
        {
            return (call, cancel) =>
            {
                var task = app(
                    call.Environment,
                    call.Headers,
                    call.Body,
                    cancel);

                return task.Then(result => new ResultParameters
                {
                    Properties = result.Item1,
                    Status = result.Item2,
                    Headers = result.Item3,
                    Body = ToDelegate(result.Item4)
                });
            };
        }

        public static BodyDelegate ToDelegate(BodyFunc body)
        {
            return (stream, cancel) => body(stream, cancel);
        }
    }
}
