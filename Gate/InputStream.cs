using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gato
{
    public class InputStream : Stream
    {
        Action cancel;

        public InputStream(Func<
            Func<ArraySegment<byte>, Action, bool>, 
            Action<Exception>, 
            Action, 
            Action> input)
        {
            cancel = input(OnNext, OnError, OnCompleted);
        }

        void OnCompleted()
        {
        }

        void OnError(Exception error)
        {
        }

        bool OnNext(ArraySegment<byte> data, Action ct)
        {
            return false;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return null;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        #region Stream boilerplate

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override void Flush() { }
        public override long Length { get { throw new NotSupportedException(); } }
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
