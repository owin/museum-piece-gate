using System;
using System.IO;
using System.Threading;

namespace Gate.Hosts.AspNet
{
    class PipeRequest
    {
        readonly Stream _stream;
        readonly Func<ArraySegment<byte>, Action, bool> _next;

        readonly byte[] _buffer;
        ArraySegment<byte> _segment;

        bool _running;

        Tuple<Action<Exception>, Action> _finalAction;
        static readonly Tuple<Action<Exception>, Action> FinalNoop = new Tuple<Action<Exception>, Action>(ex => { }, () => { });



        public PipeRequest(Stream stream, Func<ArraySegment<byte>, Action, bool> next, Action<Exception> error, Action complete)
        {
            _stream = stream;
            _next = next;
            _finalAction = Tuple.Create(error, complete);
            _buffer = new byte[1024];
        }


        public Action Go()
        {
            _running = true;
            try
            {
                Loop(0);
            }
            catch (Exception ex)
            {
                FireError(ex);
            }
            return () => _running = false;
        }

        void Loop(int mode)
        {

            while (_running)
            {
                switch (mode)
                {
                    case 0:
                        {
                            var sr = _stream.BeginRead(_buffer, 0, _buffer.Length, ReadCallback, null);
                            if (!sr.CompletedSynchronously)
                            {
                                return;
                            }

                            _segment = new ArraySegment<byte>(_buffer, 0, _stream.EndRead(sr));
                            mode = 1;
                        }
                        break;

                    case 1:
                        {
                            if (_segment.Count == 0)
                            {
                                FireComplete();
                                return;
                            }

                            if (_next(_segment, NextCallback))
                            {
                                return;
                            }

                            mode = 0;
                        }
                        break;
                }
            }
        }

        void ReadCallback(IAsyncResult ar)
        {
            if (ar.CompletedSynchronously)
            {
                return;
            }
            try
            {
                _segment = new ArraySegment<byte>(_buffer, 0, _stream.EndRead(ar));
                Loop(1);
            }
            catch (Exception ex)
            {
                FireError(ex);
            }
        }

        void NextCallback()
        {
            try
            {
                Loop(0);
            }
            catch (Exception ex)
            {
                FireError(ex);
            }
        }


        void FireError(Exception ex)
        {
            _running = false;
            Interlocked.Exchange(ref _finalAction, FinalNoop).Item1(ex);
        }

        void FireComplete()
        {
            _running = false;
            Interlocked.Exchange(ref _finalAction, FinalNoop).Item2();
        }

    }
}