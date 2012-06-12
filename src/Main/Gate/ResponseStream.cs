using System;
using System.IO;

namespace Gate
{
    public class ResponseStream : Stream
    {
        readonly Response _response;

        public ResponseStream(Response response)
        {
            _response = response;
        }

        public override void Flush()
        {
            _response.Flush();
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            _response.Write(ToArraySegment(buffer, offset, count));
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _response.BeginWrite(ToArraySegment(buffer, offset, count), callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _response.EndWrite(asyncResult);
        }

        static ArraySegment<byte> ToArraySegment(byte[] buffer, int offset, int count)
        {
            return buffer == null ? default(ArraySegment<byte>) : new ArraySegment<byte>(buffer, offset, count);
        }


        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
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
    }
}