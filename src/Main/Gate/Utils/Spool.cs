using System;
using System.Reflection;
using System.Threading;

namespace Gate.Utils
{
    public class Spool
    {
        readonly bool _eagerPull;

        public Spool()
        {
        }

        public Spool(bool eagerPull)
        {
            _eagerPull = eagerPull;
        }

        public Owin.TempEnum Push(ArraySegment<byte> data, Action<Exception> continuation)
        {
            //todo - protect against concurrent async calls
            lock (_asyncPull)
            {
                // drop onto outstanding pull operations first
                while (data.Count != 0 && _asyncPull.Data.Count != 0)
                {
                    Drain(data, _asyncPull.Data, (a, b, c) =>
                    {
                        data = a;
                        _asyncPull.Data = b;
                        _asyncPull.Retval[0] += c;
                    });
                    if (_asyncPull.Data.Count == 0 && _asyncPull.Continuation != null)
                    {
                        var pullContinuation = _asyncPull.Continuation;
                        _asyncPull.Continuation = null;
                        pullContinuation(null);
                    }
                }

                // release partially filled when eager
                if (_eagerPull && _asyncPull.Retval[0] != 0 && _asyncPull.Continuation != null)
                {
                    var pullContinuation = _asyncPull.Continuation;
                    _asyncPull.Continuation = null;
                    pullContinuation(null);
                }

                // push fully consumed
                if (data.Count == 0)
                {
                    return OwinConstants.CompletedSynchronously;
                }

                // delay if possible
                if (continuation != null)
                {
                    lock (_asyncPush)
                    {
                        _asyncPush.Data = data;
                        _asyncPush.Continuation = continuation;
                        return OwinConstants.CompletingAsynchronously;
                    }
                }

                // otherwise spool synchronously
                lock (_buffer)
                {
                    _buffer.Push(data);
                    return OwinConstants.CompletedSynchronously;
                }
            }
        }

        public void PushComplete(Exception error)
        {
            lock (_asyncPull)
            {
                _complete = true;
                _completeError = error;
                if (_asyncPull.Continuation != null)
                {
                    var pullContinuation = _asyncPull.Continuation;
                    _asyncPull.Continuation = null;
                    pullContinuation(_completeError);
                }
            }
        }

        public Owin.TempEnum Pull(ArraySegment<byte> data, int[] retval, Action<Exception> continuation)
        {
            Action exitLatch = null;
            lock (_asyncPush)
            {
                // draw from buffer and outstanding push operations first
                while (data.Count != 0 && (_buffer.Data.Count != 0 || _asyncPush.Data.Count != 0))
                {
                    lock (_buffer)
                    {
                        _buffer.Drain(data, (d1, c) =>
                        {
                            data = d1;
                            retval[0] += c;
                        });
                    }
                    if (data.Count == 0)
                    {
                        return OwinConstants.CompletedSynchronously;
                    }
                    Drain(_asyncPush.Data, data, (d0, d1, c) =>
                    {
                        _asyncPush.Data = d0;
                        data = d1;
                        retval[0] += c;
                    });
                    if (_asyncPush.Data.Count == 0 && _asyncPush.Continuation != null)
                    {
                        var pushContinuation = _asyncPush.Continuation;
                        _asyncPush.Continuation = null;
                        pushContinuation(null);
                    }
                }
            }

            // return partially filled when eager
            if (_eagerPull && retval[0] != 0)
            {
                return OwinConstants.CompletedSynchronously;
            }

            // pull fully satisfied
            if (data.Count == 0)
            {
                return OwinConstants.CompletedSynchronously;
            }

            //todo - there's a simultaneous push-pull problem entering this lock...
            lock (_asyncPull)
            {
                lock (_asyncPush)
                {
                    if (_complete)
                    {
                        if (_completeError != null)
                        {
                            throw new TargetInvocationException(_completeError);
                        }
                        return OwinConstants.CompletedSynchronously;
                    }

                    _asyncPull.Data = data;
                    _asyncPull.Retval = retval;
                    if (continuation != null)
                    {
                        _asyncPull.Continuation = continuation;
                    }
                    else
                    {
                        var latch = new ManualResetEvent(false);
                        Exception latchError = null;
                        _asyncPull.Continuation = error =>
                        {
                            latchError = error;
                            latch.Set();
                        };
                        exitLatch = () =>
                        {
                            latch.WaitOne();
                            if (latchError != null)
                            {
                                throw new TargetInvocationException(latchError);
                            }
                        };
                    }
                }
            }


            if (exitLatch != null)
            {
                exitLatch();
                return OwinConstants.CompletedSynchronously;
            }

            return OwinConstants.CompletingAsynchronously;
        }


        static void Drain(
            ArraySegment<byte> source,
            ArraySegment<byte> destination,
            Action<ArraySegment<byte>, ArraySegment<byte>, int> result)
        {
            var copied = Math.Min(source.Count, destination.Count);
            if (copied == 0) return;
            Array.Copy(source.Array, source.Offset, destination.Array, destination.Offset, copied);
            result(
                source.Count == copied ? Empty : new ArraySegment<byte>(source.Array, source.Offset + copied, source.Count - copied),
                destination.Count == copied ? Empty : new ArraySegment<byte>(destination.Array, destination.Offset + copied, destination.Count - copied),
                copied);
        }

        static readonly ArraySegment<byte> Empty = new ArraySegment<byte>(new byte[0], 0, 0);

        readonly AsyncOp _asyncPush = new AsyncOp();
        readonly AsyncOp _asyncPull = new AsyncOp();

        class AsyncOp
        {
            public AsyncOp()
            {
                Data = Empty;
                Retval = new int[1];
            }

            public ArraySegment<byte> Data { get; set; }
            public int[] Retval { get; set; }
            public Action<Exception> Continuation { get; set; }
        }


        readonly Buffer _buffer = new Buffer();
        bool _complete;
        Exception _completeError;

        class Buffer
        {
            public Buffer()
            {
                Data = Empty;
            }

            public ArraySegment<byte> Data { get; set; }

            public void Push(ArraySegment<byte> data)
            {
                //TODO- rolling spool pages - spooling to a contiguous array is temporary
                var concat = new ArraySegment<byte>(new byte[Data.Count + data.Count]);
                Array.Copy(Data.Array, Data.Offset, concat.Array, 0, Data.Count);
                Array.Copy(data.Array, data.Offset, concat.Array, Data.Count, data.Count);
                Data = concat;
            }

            public void Drain(ArraySegment<byte> data, Action<ArraySegment<byte>, int> result)
            {
                var copied = Math.Min(data.Count, Data.Count);
                if (copied == 0) return;
                Array.Copy(Data.Array, Data.Offset, data.Array, data.Offset, copied);
                Data = new ArraySegment<byte>(Data.Array, Data.Offset + copied, Data.Count - copied);
                result(new ArraySegment<byte>(data.Array, data.Offset + copied, data.Count - copied), copied);
            }
        }
    }
}