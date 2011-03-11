using System;
using System.IO;

namespace Gate
{
    public class OutputStream : Stream
    {
        Func<ArraySegment<byte>, Action, bool> next;
        Action complete;
        AsyncResult asyncResult;
        bool completed;

        public OutputStream(Func<ArraySegment<byte>, Action, bool> next, Action complete)
        {
            if (next == null)
                throw new ArgumentNullException("next");
            if (complete == null)
                throw new ArgumentNullException("complete");

            this.next = next;
            this.complete = complete;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureNotCompleted();

            next(new ArraySegment<byte>(buffer, offset, count), (Action)null);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            EnsureNotCompleted();

            asyncResult = new AsyncResult(callback, state);

            if (!next(new ArraySegment<byte>(buffer, offset, count), () => asyncResult.SetAsCompleted(null, false)))
                asyncResult.SetAsCompleted(null, true);

            return asyncResult;
        }

        public override void EndWrite(IAsyncResult ar)
        {
            if (asyncResult != ar)
                throw new ArgumentException("Invalid IAsyncResult argument.");

            asyncResult.EndInvoke();
        }

        public override void Close()
        {
            EnsureNotCompleted();
            completed = true;
            complete();
        }

        public override void Flush() { }

        void EnsureNotCompleted()
        {
            if (completed) throw new InvalidOperationException("The stream was completed.");
        }

        #region Stream boilerplate

        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }

        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
