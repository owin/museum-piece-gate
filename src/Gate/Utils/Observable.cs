using System;

namespace Gate.Utils
{
    public static class ObservableBody
    {
        public static IObservable<Tuple<ArraySegment<byte>, Action, Action>> Create(
            Func<Action<Tuple<ArraySegment<byte>, Action, Action>>, Action<Exception>, Action, Action> subscribe)
        {
            return Create((next, error, complete) => new Disposable(subscribe(next, error, complete)));
        }

        public static IObservable<Tuple<ArraySegment<byte>, Action, Action>> Create(
            Func<Action<Tuple<ArraySegment<byte>, Action, Action>>, Action<Exception>, Action, IDisposable> subscribe)
        {
            return new Observable<Tuple<ArraySegment<byte>, Action, Action>>(subscribe);
        }

        public static IObservable<Tuple<ArraySegment<byte>, Action, Action>> Create(
            Func<IObserver<Tuple<ArraySegment<byte>, Action, Action>>, Action> subscribe)
        {
            return Create(observer => new Disposable(subscribe(observer)));
        }

        public static IObservable<Tuple<ArraySegment<byte>, Action, Action>> Create(
            Func<IObserver<Tuple<ArraySegment<byte>, Action, Action>>, IDisposable> subscribe)
        {
            return new Observable<Tuple<ArraySegment<byte>, Action, Action>>(subscribe);
        }


        public static void OnNext(this IObserver<Tuple<ArraySegment<byte>, Action, Action>> observer, ArraySegment<byte> data)
        {
            observer.OnNext(new Tuple<ArraySegment<byte>, Action, Action>(data, null, null));
        }

        public static void OnNext(this IObserver<Tuple<ArraySegment<byte>, Action, Action>> observer, ArraySegment<byte> data, Action pause, Action continuation)
        {
            observer.OnNext(new Tuple<ArraySegment<byte>, Action, Action>(data, pause, continuation));
        }

        public static bool OnNext(this IObserver<Tuple<ArraySegment<byte>, Action, Action>> observer, ArraySegment<byte> data, Action continuation)
        {
            if (continuation == null)
            {
                observer.OnNext(new Tuple<ArraySegment<byte>, Action, Action>(data, null, null));
                return false;
            }

            var paused = false;
            observer.OnNext(new Tuple<ArraySegment<byte>, Action, Action>(data, () => paused = true, continuation));
            return paused;
        }


        class Observable<T> : IObservable<T>
        {
            readonly Func<IObserver<T>, IDisposable> _subscribe;

            public Observable(Func<Action<T>, Action<Exception>, Action, IDisposable> subscribe)
            {
                _subscribe = observer => subscribe(observer.OnNext, observer.OnError, observer.OnCompleted);
            }

            public Observable(Func<IObserver<T>, IDisposable> subscribe)
            {
                _subscribe = subscribe;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return _subscribe(observer);
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