using System;
using System.IO;
using System.Threading;
using Gate.Owin;

namespace Gate.Hosts.AspNet
{
    class PipeResponse
    {
        readonly Stream _stream;
        readonly Action<Exception> _error;
        readonly Action _complete;

        public PipeResponse(Stream stream, Action<Exception> error, Action complete)
        {
            _stream = stream;
            _error = error;
            _complete = complete;
        }

        public void Go(BodyDelegate body)
        {
            body(OnWrite, OnFlush, OnEnd, CancellationToken);
        }

        bool OnWrite(ArraySegment<byte> data)
        {
            _stream.Write(data.Array, data.Offset, data.Count);
            return false;
        }

        bool OnFlush(Action continuation)
        {
            _stream.Flush();
            return false;
        }

        void OnEnd(Exception exception)
        {
            if (exception == null)
                _complete();
            else
                _error(exception);
        }

        protected CancellationToken CancellationToken { get; set; }
    }
}