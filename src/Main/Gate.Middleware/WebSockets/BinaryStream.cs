using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Gate.Middleware.WebSockets
{
    #pragma warning disable 811
    using WebSocketFunc =
        Func
        <
        // SendAsync
            Func
            <
                ArraySegment<byte> /* data */,
                int /* messageType */,
                bool /* endOfMessage */,
                CancellationToken /* cancel */,
                Task
            >,
        // ReceiveAsync
            Func
            <
                ArraySegment<byte> /* data */,
                CancellationToken /* cancel */,
                Task
                <
                    Tuple
                    <
                        int /* messageType */,
                        bool /* endOfMessage */,
                        int? /* count */,
                        int? /* closeStatus */,
                        string /* closeStatusDescription */
                    >
                >
            >,
        // CloseAsync
            Func
            <
                int /* closeStatus */,
                string /* closeDescription */,
                CancellationToken /* cancel */,
                Task
            >,
        // Complete
            Task
        >;
    #pragma warning restore 811

    using WebSocketSendAsync =
        Func
            <
                ArraySegment<byte> /* data */,
                int /* messageType */,
                bool /* endOfMessage */,
                CancellationToken /* cancel */,
                Task
            >;

    using WebSocketReceiveAsync =
        Func
            <
                ArraySegment<byte> /* data */,
                CancellationToken /* cancel */,
                Task
                <
                    Tuple
                    <
                        int /* messageType */,
                        bool /* endOfMessage */,
                        int? /* count */,
                        int? /* closeStatus */,
                        string /* closeStatusDescription */
                    >
                >
            >;

    using WebSocketReceiveTuple =
        Tuple
            <
                int /* messageType */,
                bool /* endOfMessage */,
                int? /* count */,
                int? /* closeStatus */,
                string /* closeStatusDescription */
            >;
    
    using WebSocketCloseAsync =
        Func
            <
                int /* closeStatus */,
                string /* closeDescription */,
                CancellationToken /* cancel */,
                Task
            >;

    // This class allows a developer to produce and consume websocket data as if it were a binary stream.
    internal class BinaryStream : Stream
    {
        private const int closeCode = 0x8;

        private WebSocketSendAsync sendAsync;
        private WebSocketReceiveAsync receiveAsync;
        private WebSocketCloseAsync closeAsync;
        private int messageType; // 0x1 for Text, 0x2 for binary

        public BinaryStream(WebSocketSendAsync sendAsync, WebSocketReceiveAsync receiveAsync, WebSocketCloseAsync closeAsync, int messageType)
        {
            this.sendAsync = sendAsync;
            this.receiveAsync = receiveAsync;
            this.closeAsync = closeAsync;
            this.messageType = messageType;
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
            get { return true; }
        }

        public override void Flush()
        {
            // throw new NotImplementedException();
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
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
            // TODO: Cancellation
            // Block
            WebSocketReceiveTuple result = receiveAsync(new ArraySegment<byte>(buffer, offset, count), CancellationToken.None).Result;

            // Check for close
            if (result.Item1 == closeCode)
            {
                // End of incoming data, expect future reads to fail.
                return 0;
            }
            return result.Item3.Value; // read
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancel)
        {
            Task<WebSocketReceiveTuple> receiveTask = receiveAsync(new ArraySegment<byte>(buffer, offset, count), cancel);
            Task<int> resultTask = receiveTask.Then<WebSocketReceiveTuple, int>(
                result =>
                {
                    // Check for close
                    if (result.Item1 == closeCode)
                    {
                        // End of incoming data, expect future reads to fail.
                        return 0;
                    }
                    return result.Item3.Value; // read
                });
            return resultTask;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            sendAsync(new ArraySegment<byte>(buffer, offset, count), messageType, true, CancellationToken.None).Wait();
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancel)
        {
            return sendAsync(new ArraySegment<byte>(buffer, offset, count), messageType, true, cancel);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                closeAsync(1000, string.Empty, CancellationToken.None).Wait();
            }
        }
    }
}
