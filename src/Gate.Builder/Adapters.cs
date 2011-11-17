using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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


    public static class Adapters
    {
        public static AppAction ToAction(AppDelegate app)
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

        public static AppDelegate ToDelegate(AppAction app)
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

        public static BodyAction ToAction(BodyDelegate body)
        {
            return (next, error, complete) => body(next, error, complete);
        }

        public static BodyDelegate ToDelegate(BodyAction body)
        {
            return (next, error, complete) => body(next, error, complete);
        }


        public static OwinApp ToApp(AppDelegate app)
        {
            return
                env =>
                {
                    var tcs = new TaskCompletionSource<OwinResult>();
                    app(
                        env,
                        (status, headers, body) => tcs.SetResult(new OwinResult(status, headers, ToObservable(body))),
                        tcs.SetException);
                    return tcs.Task;
                };
        }

        public static AppDelegate ToDelegate(OwinApp app)
        {
            return
                (env, result, fault) =>
                {
                    var task = app(env);
                    task.ContinueWith(
                        t => result(t.Result.Status, t.Result.Headers, ToDelegate(t.Result.Body)),
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
                    task.ContinueWith(
                        t => fault(t.Exception),
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
                };
        }


        public static IObservable<OwinData> ToObservable(BodyDelegate body)
        {
            return new Observable((next, error, complete) =>
                body((bytes, resume) =>
                {
                    var paused = false;
                    next(new OwinData(bytes, () => paused = true, resume));
                    return paused;
                }, error, complete));
        }

        public static BodyDelegate ToDelegate(IObservable<OwinData> body)
        {
            return (next, error, complete) =>
                body.Subscribe(
                    new Observer(
                        data =>
                        {
                            if (next(data.Bytes, data.Resume))
                                data.Pause();
                        },
                        error,
                        complete)).Dispose;
        }



        class Observable : IObservable<OwinData>
        {
            readonly Func<Action<OwinData>, Action<Exception>, Action, Action> _subscribe;

            public Observable(Func<Action<OwinData>, Action<Exception>, Action, Action> subscribe)
            {
                _subscribe = subscribe;
            }

            public IDisposable Subscribe(IObserver<OwinData> observer)
            {
                return new Disposable(_subscribe(observer.OnNext, observer.OnError, observer.OnCompleted));
            }
        }

        class Observer : IObserver<OwinData>
        {
            readonly Action<OwinData> _next;
            readonly Action<Exception> _error;
            readonly Action _completed;

            public Observer(Action<OwinData> next, Action<Exception> error, Action completed)
            {
                _next = next;
                _error = error;
                _completed = completed;
            }

            public void OnNext(OwinData value)
            {
                _next(value);
            }

            public void OnError(Exception error)
            {
                _error(error);
            }

            public void OnCompleted()
            {
                _completed();
            }
        }

        class Disposable : IDisposable
        {
            readonly Action _dispose;

            public Disposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose();
            }
        }
    }


}
