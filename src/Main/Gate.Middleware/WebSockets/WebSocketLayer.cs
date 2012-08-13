using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Gate.Middleware.WebSockets
{
    using WebSocketReceiveTuple =
        Tuple
            <
                int /* messageType */,
                bool /* endOfMessage */,
                int? /* count */,
                int? /* closeStatus */,
                string /* closeStatusDescription */
            >;

    // This class implements the WebSocket layer on top of an opaque stream.
    public class WebSocketLayer
    {
        private Stream incoming;
        private Stream outgoing;

        public WebSocketLayer(Stream incoming, Stream outgoing)
        {
            this.incoming = incoming;
            this.outgoing = outgoing;
        }

        // Add framing and send the data.  One frame per call to Send.
        public Task SendAsync(ArraySegment<byte> buffer, int messageType, bool endOfMessage, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        // Receive frames, unmask them.
        // Should handle pings/pongs internally.
        // Should parse out Close frames.
        public Task<WebSocketReceiveTuple> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        // Send a close frame.  The WebSocket is not actually considered closed until a close frame has been both sent and received.
        public Task CloseAsync(int status, string description, CancellationToken cancel)
        {
            // This could just be a wrapper around SendAsync, or at least they could share an internal helper send method.
            throw new NotImplementedException();
        }

        // Shutting down.  Send a close frame if one has been received but not set. Otherwise abort (fail the Task).
        public Task CleanupAsync()
        {
            throw new NotImplementedException();
        }
    }
}
