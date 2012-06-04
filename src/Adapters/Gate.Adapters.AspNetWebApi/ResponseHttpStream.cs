using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gate.Adapters.AspNetWebApi
{
    class ResponseHttpStream : StreamNotImpl
    {
        private readonly Func<ArraySegment<byte>, Action, bool> _write;
        private Action<Exception> _end;

        public ResponseHttpStream(
            Func<ArraySegment<byte>, Action, bool> write,
            Action<Exception> end)
        {
            _write = write;
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
            _write(new ArraySegment<byte>(buffer, offset, count), null);
        }

        public override void Flush()
        {
            _write(new ArraySegment<byte>(null, 0, 0), null);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<object>(state);
            if (callback != null)
            {
                tcs.Task.ContinueWith(_ => callback(tcs.Task)).Catch();
            }
            if (!_write(new ArraySegment<byte>(buffer, offset, count), () => tcs.TrySetResult(null)))
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