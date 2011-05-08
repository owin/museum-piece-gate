using System;
using System.IO;
using System.Web;

namespace Gate.AspNet.Tests
{
    public class RemoteHostClosedStream : Stream
    {
        public override void Flush()
        {
            throw new HttpException("The remote host closed the connection");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new HttpException("The remote host closed the connection");
        }

        public override void SetLength(long value)
        {
            throw new HttpException("The remote host closed the connection");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new HttpException("The remote host closed the connection");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new HttpException("The remote host closed the connection");
        }

        public override bool CanRead
        {
            get { throw new HttpException("The remote host closed the connection"); }
        }

        public override bool CanSeek
        {
            get { throw new HttpException("The remote host closed the connection"); }
        }

        public override bool CanWrite
        {
            get { throw new HttpException("The remote host closed the connection"); }
        }

        public override long Length
        {
            get { throw new HttpException("The remote host closed the connection"); }
        }

        public override long Position
        {
            get { throw new HttpException("The remote host closed the connection"); }
            set { throw new HttpException("The remote host closed the connection"); }
        }
    }
}