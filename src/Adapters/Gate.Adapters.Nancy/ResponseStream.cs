using System;
using System.IO;
using System.Threading;

namespace Gate.Adapters.Nancy
{
    class ResponseStream : Stream
    {
        Func<ArraySegment<byte>, Action, bool> _next;
        Action _complete;

        public ResponseStream(Func<ArraySegment<byte>, Action, bool> next, Action complete)
        {
            _next = next;
            _complete = complete;
        }

        public override void Close()
        {
            _next = (_, __) => false;
            Interlocked.Exchange(ref _complete, ()=> { }).Invoke();
        }

        public override void Flush()
        {
            _next(new ArraySegment<byte>(new byte[0]), null);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _next(new ArraySegment<byte>(buffer, offset, count), null);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var result = new WriteAsyncResult { AsyncState = state };
            var delayed = _next(
                new ArraySegment<byte>(buffer, offset, count),
                () =>
                {
                    result.IsCompleted = true;
                    if (callback != null)
                    {
                        try { callback(result); }
                        catch { }
                    }
                });

            if (!delayed)
            {
                result.CompletedSynchronously = true;
                result.IsCompleted = true;
                if (callback != null)
                {
                    try { callback(result); }
                    catch { }
                }
            }

            return result;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (!asyncResult.IsCompleted)
            {
                throw new InvalidOperationException("Async write with wait state is not supported");
            }
        }

        public class WriteAsyncResult : IAsyncResult
        {
            public bool IsCompleted { get; set; }
            public WaitHandle AsyncWaitHandle { get { throw new InvalidOperationException("Async write with wait state is not supported"); } }
            public object AsyncState { get; set; }
            public bool CompletedSynchronously { get; set; }
        }

        public override void WriteByte(byte value)
        {
            Write(new[] { value }, 0, 1);
        }



        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }


        public override int ReadByte()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override int ReadTimeout
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override int WriteTimeout
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}