using System;
using System.IO;
using System.Threading;
using Gate.Owin;

namespace Gate.HttpListener
{
    class PipeResponse
    {
        readonly Stream _stream;

        bool _earlyCancel;
        Action _cancel;
        static readonly Action CancelNoop = () => { };

        Tuple<Action<Exception>, Action> _finalAction;
        static readonly Tuple<Action<Exception>, Action> FinalNoop = new Tuple<Action<Exception>, Action>(ex => { }, () => { });

        public PipeResponse(Stream stream, Action<Exception> error, Action complete)
        {
            _stream = stream;
            _finalAction = Tuple.Create(error, complete);
            _cancel = () => _earlyCancel = true;
        }

        public void Go(BodyDelegate body)
        {
            Interlocked.Exchange(ref _cancel, body(OnNext, OnError, OnComplete));
            if (_earlyCancel)
                FireCancel();
        }

        bool OnNext(ArraySegment<byte> data, Action continuation)
        {
            try
            {
                if (data.Array == null || data.Array.Length == 0)
                {
                    _stream.Flush();
                    return false;
                }

                if (continuation == null)
                {
                    _stream.Write(data.Array, data.Offset, data.Count);
                    return false;
                }

                var sr = _stream.BeginWrite(data.Array, data.Offset, data.Count, ar =>
                {
                    if (ar.CompletedSynchronously) return;
                    try
                    {
                        _stream.EndWrite(ar);
                    }
                    catch (Exception ex)
                    {
                        FireError(ex);
                    }
                    finally
                    {
                        continuation();
                    }
                }, null);

                if (!sr.CompletedSynchronously)
                    return true;

                _stream.EndWrite(sr);
            }
            catch (Exception ex)
            {
                FireError(ex);
            }

            return false;
        }

        void OnError(Exception ex)
        {
            FireError(ex);
        }

        void OnComplete()
        {
            FireComplete();
        }

        void FireCancel()
        {
            try
            {
                Interlocked.Exchange(ref _cancel, CancelNoop).Invoke();
            }
            catch (Exception ex)
            {
                // logger of last resort
            }
        }

        void FireError(Exception ex)
        {
            try
            {
                Interlocked.Exchange(ref _finalAction, FinalNoop).Item1(ex);
            }
            catch (Exception ex2)
            {
                // logger of last resort
            }
            FireCancel();
        }

        void FireComplete()
        {
            try
            {
                Interlocked.Exchange(ref _finalAction, FinalNoop).Item2();
            }
            catch (Exception ex)
            {
                // logger of last resort
            }
        }
    }
}