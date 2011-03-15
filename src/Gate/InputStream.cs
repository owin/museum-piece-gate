using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gate.Spooling;

namespace Gate
{
    public class InputStream : Stream
    {
        Spool _spool = new Spool();

        Action cancel;

        public InputStream(Func<
            Func<ArraySegment<byte>, Action, bool>,
            Action<Exception>,
            Action,
            Action> input)
        {
            cancel = input(OnNext, OnError, OnCompleted);
        }

        bool OnNext(ArraySegment<byte> data, Action ct)
        {
            return _spool.Push(data, ct);
        }

        void OnCompleted()
        {
            _spool.PushComplete();
        }

        void OnError(Exception error)
        {
            //todo - pass exception through to next read
            _spool.PushComplete();
        }

        public override void Close() {
            cancel();
            base.Close();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var asyncResult = new AsyncResult<int>(callback, state);
            var retval = new int[1];
            var async = _spool.Pull(new ArraySegment<byte>(buffer, offset, count), retval, () => asyncResult.SetAsCompleted(retval[0], false));
            if (async == false)
                asyncResult.SetAsCompleted(retval[0], true);
            return asyncResult;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((AsyncResult<int>) asyncResult).EndInvoke();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var retval = new int[1];
            _spool.Pull(new ArraySegment<byte>(buffer, offset, count), retval, null);
            return retval[0];
        }

        #region Stream boilerplate

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}