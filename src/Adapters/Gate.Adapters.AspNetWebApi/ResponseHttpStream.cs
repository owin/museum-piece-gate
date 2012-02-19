using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gate.Adapters.AspNetWebApi
{
    class ResponseHttpStream : StreamNotImpl
    {
        private readonly Func<ArraySegment<byte>, bool> _write;
        private readonly Func<Action, bool> _flush;
        private Action<Exception> _end;

        public ResponseHttpStream(
            Func<ArraySegment<byte>, bool> write,
            Func<Action, bool> flush,
            Action<Exception> end)
        {
            _write = write;
            _flush = flush;
            _end = end;
        }

        public override void Close()
        {
            Interlocked.Exchange(ref _end, error => { }).Invoke(null);
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _write(new ArraySegment<byte>(buffer, offset, count));
        }

        public override void Flush()
        {
            _flush(null);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<object>(state);
            if (callback != null)
            {
                tcs.Task.ContinueWith(_ => callback(tcs.Task)).Catch();
            }
            if (!_write(new ArraySegment<byte>(buffer, offset, count)) ||
                !_flush(() => tcs.TrySetResult(null)))
            {
                tcs.TrySetResult(null);
            }
            return tcs.Task;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            ((Task)asyncResult).Wait();
        }
    }
}