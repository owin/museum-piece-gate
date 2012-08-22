using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Owin;
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

    using OpaqueStreamFunc =
        Func
        <
            Stream, // Incoming (CanRead)
            Stream, // Outgoing (CanWrite)            
            Task // Complete
        >;

    // This class demonstrates how to support WebSockets on a server that only supports opaque streams.
    // WebSocket Extension v0.2 is currently implemented.
    public static class OpaqueToWebSocket
    {
        public static IAppBuilder UseWebSockets(this IAppBuilder builder)
        {
            return builder.UseFunc<AppDelegate>(OpaqueToWebSocket.Middleware);
        }

        public static AppDelegate Middleware(AppDelegate app)
        {
            return call =>
            {
                string opaqueSupport = call.Environment.Get<string>("opaque.Support");
                string websocketSupport = call.Environment.Get<string>("websocket.Support");
                if (opaqueSupport == "OpaqueStreamFunc" && websocketSupport != "WebSocketFunc" && IsWebSocketRequest(call))
                {
                    // Announce websocket support
                    call.Environment["websocket.Support"] = "WebSocketFunc";

                    return app(call).Then(result =>
                    {
                        if (result.Status == 101
                            && result.Properties != null
                            && result.Properties.Get<WebSocketFunc>("websocket.Func") != null)
                        {
                            result.Headers = result.Headers ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                            SetWebSocketResponseHeaders(call, result);

                            WebSocketFunc wsBody = result.Properties.Get<WebSocketFunc>("websocket.Func");

                            OpaqueStreamFunc opaqueBody = (incoming, outgoing) =>
                            {
                                WebSocketLayer webSocket = new WebSocketLayer(incoming, outgoing);
                                return wsBody(webSocket.SendAsync, webSocket.ReceiveAsync, webSocket.CloseAsync)
                                    .Then(() => webSocket.CleanupAsync());
                            };

                            result.Properties["opaque.Func"] = opaqueBody;
                        }
                        return result;
                    });
                }
                else
                {
                    return app(call);
                }
            };
        }

        // Inspect the method and headers to see if this is a valid websocket request.
        // See RFC 6455 section 4.2.1.
        private static bool IsWebSocketRequest(CallParameters call)
        {
            throw new NotImplementedException();
        }

        // Se the websocket response headers.
        // See RFC 6455 section 4.2.2.
        private static void SetWebSocketResponseHeaders(CallParameters call, ResultParameters result)
        {
            throw new NotImplementedException();
        }
    }
}
