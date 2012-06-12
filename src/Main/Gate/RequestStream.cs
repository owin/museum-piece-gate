using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gate.Utils;
using Owin;

namespace Gate
{
    public class RequestStream : Stream
    {
        readonly Spool _spool;

        public RequestStream()
        {
            _spool = new Spool(true);
        }

        public void Start(BodyDelegate body, CancellationToken cancel)
        {
            body.Invoke(OnWrite, OnEnd, cancel);
        }

        bool OnWrite(ArraySegment<byte> data, Action callback)
        {
            return _spool.Push(data, callback);
        }

        void OnEnd(Exception error)
        {
            _spool.PushComplete();
        }

        public override void Flush()
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
            var retval = new int[1];
            _spool.Pull(new ArraySegment<byte>(buffer, offset, count), retval, null);
            return retval[0];
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            var retval = new int[1];
            if (!_spool.Pull(new ArraySegment<byte>(buffer, offset, count), retval, () => tcs.SetResult(retval[0])))
            {
                tcs.SetResult(retval[0]);
            }
            tcs.Task.ContinueWith(t => callback(t));
            return tcs.Task;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((Task<int>)asyncResult).Result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

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

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}