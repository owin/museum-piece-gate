using System;
using System.Threading;

namespace Gate.Spooling
{
    public class Spool
    {
        public bool Push(ArraySegment<byte> data, Action continuation)
        {
            //todo - potential fatal embrace
            //todo - protect against concurrent async push
            lock (_asyncPull)
            {
                // drop onto outstanding pull operations first
                while (data.Count != 0 && _asyncPull.Data.Count != 0)
                {
                    Drain(data, _asyncPull.Data, (a, b) =>
                    {
                        data = a;
                        _asyncPull.Data = b;
                    });
                    if (_asyncPull.Data.Count == 0 && _asyncPull.Continuation != null)
                    {
                        var pullContinuation = _asyncPull.Continuation;
                        _asyncPull.Continuation = null;
                        pullContinuation();
                    }
                }

                // push fully consumed
                if (data.Count == 0)
                {
                    return false;
                }

                // delay if possible
                if (continuation != null)
                {
                    lock (_asyncPush)
                    {
                        _asyncPush.Data = data;
                        _asyncPush.Continuation = continuation;
                        return true;
                    }
                }

                // otherwise spool synchronously
                lock (_buffer)
                {
                    _buffer.Push(data);
                    return false;
                }
            }
        }


        public bool Pull(ArraySegment<byte> data, Action continuation)
        {
            Action exitGate = null;
            lock (_asyncPush)
            {
                // draw from buffer and outstanding push operations first
                while (data.Count != 0 && (_buffer.Data.Count != 0 || _asyncPush.Data.Count != 0))
                {
                    lock (_buffer)
                    {
                        _buffer.Drain(data, d1 => { data = d1; });
                    }
                    if (data.Count == 0) return false;
                    Drain(_asyncPush.Data, data, (d0, d1) =>
                    {
                        _asyncPush.Data = d0;
                        data = d1;
                    });
                    if (_asyncPush.Data.Count == 0 && _asyncPush.Continuation != null)
                    {
                        var pushContinuation = _asyncPush.Continuation;
                        _asyncPush.Continuation = null;
                        pushContinuation();
                    }
                }
            }
            lock (_asyncPull)
            {
                lock (_asyncPush)
                {
                    // pull fully satisfied
                    if (data.Count == 0)
                    {
                        return false;
                    }

                    lock (_asyncPull)
                    {
                        _asyncPull.Data = data;
                        if (continuation != null)
                        {
                            _asyncPull.Continuation = continuation;
                        }
                        else
                        {
                            var gate = new ManualResetEvent(false);
                            _asyncPull.Continuation = () => { gate.Set(); };
                            exitGate = () => { gate.WaitOne(); };
                        }
                    }
                }
            }


            if (exitGate != null)
            {
                exitGate();
                return false;
            }

            return true;
        }

        
        static void Drain(
            ArraySegment<byte> source,
            ArraySegment<byte> destination,
            Action<ArraySegment<byte>, ArraySegment<byte>> result)
        {
            var copied = Math.Min(source.Count, destination.Count);
            if (copied == 0) return;
            Array.Copy(source.Array, source.Offset, destination.Array, destination.Offset, copied);
            result(
                source.Count == copied ? Empty : new ArraySegment<byte>(source.Array, source.Offset + copied, source.Count - copied),
                destination.Count == copied ? Empty : new ArraySegment<byte>(destination.Array, destination.Offset + copied, destination.Count - copied));
        }

        static readonly ArraySegment<byte> Empty = new ArraySegment<byte>(new byte[0], 0, 0);

        readonly AsyncOp _asyncPush = new AsyncOp();
        readonly AsyncOp _asyncPull = new AsyncOp();

        class AsyncOp
        {
            public AsyncOp()
            {
                Data = Empty;
            }

            public ArraySegment<byte> Data;
            public Action Continuation { get; set; }
        }


        readonly Buffer _buffer = new Buffer();

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

            public void Drain(ArraySegment<byte> data, Action<ArraySegment<byte>> result)
            {
                var copied = Math.Min(data.Count, Data.Count);
                if (copied == 0) return;
                Array.Copy(Data.Array, Data.Offset, data.Array, data.Offset, copied);
                Data = new ArraySegment<byte>(Data.Array, Data.Offset + copied, Data.Count - copied);
                result(new ArraySegment<byte>(data.Array, data.Offset + copied, data.Count - copied));
            }
        }
    }
}