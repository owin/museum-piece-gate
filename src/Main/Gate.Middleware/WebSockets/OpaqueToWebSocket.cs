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
    using AppFunc = Func<IDictionary<string, object>, Task>;

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
            return builder.UseFunc<AppFunc>(OpaqueToWebSocket.Middleware);
        }

        public static AppFunc Middleware(AppFunc app)
        {
            return env =>
            {
                string opaqueSupport = env.Get<string>("opaque.Support");
                string websocketSupport = env.Get<string>("websocket.Support");
                if (opaqueSupport == "OpaqueStreamFunc" && websocketSupport != "WebSocketFunc" && IsWebSocketRequest(env))
                {
                    // Announce websocket support
                    env["websocket.Support"] = "WebSocketFunc";

                    return app(env).Then(() =>
                    {
                        Response response = new Response(env);
                        if (response.StatusCode == 101
                            && env.Get<WebSocketFunc>("websocket.Func") != null)
                        {
                            SetWebSocketResponseHeaders(env);

                            WebSocketFunc wsBody = env.Get<WebSocketFunc>("websocket.Func");

                            OpaqueStreamFunc opaqueBody = (incoming, outgoing) =>
                            {
                                WebSocketLayer webSocket = new WebSocketLayer(incoming, outgoing);
                                return wsBody(webSocket.SendAsync, webSocket.ReceiveAsync, webSocket.CloseAsync)
                                    .Then(() => webSocket.CleanupAsync());
                            };

                            env["opaque.Func"] = opaqueBody;
                        }
                    });
                }
                else
                {
                    return app(env);
                }
            };
        }

        // Inspect the method and headers to see if this is a valid websocket request.
        // See RFC 6455 section 4.2.1.
        private static bool IsWebSocketRequest(IDictionary<string, object> env)
        {
            throw new NotImplementedException();
        }

        // Se the websocket response headers.
        // See RFC 6455 section 4.2.2.
        private static void SetWebSocketResponseHeaders(IDictionary<string, object> env)
        {
            throw new NotImplementedException();
        }
    }
}
