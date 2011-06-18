using System;
using System.IO;
using Gate.Utils;

namespace Gate.Helpers
{
    public class InputStream : Stream
    {
        readonly Spool _spool = new Spool();
        Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action> _input;
        Action _cancel;

        public InputStream(Func<
            Func<ArraySegment<byte>, Action, bool>,
            Action<Exception>,
            Action,
            Action> input)
        {
            _input = input;
        }

        void EnsureStarted()
        {
            if (_input != null)
            {
                _cancel = _input(OnNext, OnError, OnCompleted);
                _input = null;
            }
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

        public override void Close()
        {
            if (_cancel != null)
            {
                _cancel();
                _cancel = null;
            }
            base.Close();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            EnsureStarted();

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
            EnsureStarted();

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