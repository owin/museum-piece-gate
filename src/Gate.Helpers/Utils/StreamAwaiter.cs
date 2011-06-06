using System;
using System.IO;

namespace Gate.Helpers
{
    internal class StreamAwaiter
    {
        readonly Func<IAsyncResult, int> _end;
        IAsyncResult _result;
        Action _continuation;

        StreamAwaiter(Func<IAsyncResult, int> end)
        {
            _end = end;
        }

        public static StreamAwaiter Write(Stream stream, byte[] buffer, int offset, int count)
        {
            var awaiter = new StreamAwaiter(result =>
            {
                stream.EndWrite(result);
                return 0;
            });

            var sr = stream.BeginWrite(buffer, offset, count, ar => { if (!ar.CompletedSynchronously) awaiter.SetResult(ar); }, null);

            if (sr.CompletedSynchronously) awaiter.SetResult(sr);

            return awaiter;
        }

        public static StreamAwaiter Read(Stream stream, byte[] buffer, int offset, int count)
        {
            var awaiter = new StreamAwaiter(stream.EndRead);

            var sr = stream.BeginRead(buffer, offset, count, ar => { if (!ar.CompletedSynchronously) awaiter.SetResult(ar); }, null);

            if (sr.CompletedSynchronously) awaiter.SetResult(sr);

            return awaiter;
        }

        public bool BeginAwait(Action continuation)
        {
            lock (this)
            {
                if (_result != null)
                    return false;
                _continuation = continuation;
                return true;
            }
        }

        public int EndAwait()
        {
            return _end(_result);
        }

        void SetResult(IAsyncResult result)
        {
            lock (this)
            {
                _result = result;
                if (_continuation != null)
                    _continuation();
            }
        }
    }
}