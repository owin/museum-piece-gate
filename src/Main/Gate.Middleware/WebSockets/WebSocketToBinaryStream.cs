using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owin;
using System.Threading;
using System.Threading.Tasks;

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

    // This middleware demonstrates how to implement an binary stream over a WebSocket.
    public class WebSocketToBinaryStream
    {
        private const int textOpCode = 0x1;
        private const int binaryOpCode = 0x2;

        private int messageType;

        public WebSocketToBinaryStream(int messageType)
        {
            this.messageType = messageType;
        }
        /*
        public static IAppBuilder UseOpaqueStreams(this IAppBuilder builder, int messageType = binaryOpCode)
        {
            return builder.Use<AppFunc>(new WebSocketToBinaryStream(messageType).Middleware);
        }
        */
        public AppFunc Middleware(AppFunc app)
        {
            return env =>
            {
                string binarySupport = env.Get<string>("binarystream.Support");
                string websocketSupport = env.Get<string>("websocket.Support");
                if (websocketSupport != null && websocketSupport.Equals("WebSocketFunc") 
                    && (binarySupport == null || !binarySupport.Equals("BinaryStreamFunc")))
                {
                    env["binarystream.Support"] = "BinaryStreamFunc";

                    throw new NotImplementedException();
                    /*
                    return app(call).Then(result =>
                        {
                            
                            if (result.Status == 101
                                && result.Body != null
                                && result.Properties != null

                                && result.Properties.Get< >("binarystream.Func") == "BinaryStream")
                            {
                                WebSocketFunc body = (sendAsync, receiveAsync, closeAsync) =>
                                    {
                                        BinaryStream stream = new BinaryStream(sendAsync, receiveAsync, closeAsync, messageType);
                                        return result.Body(stream);
                                    };

                                result.Properties["websocket.BodyFunc"] = body;
                            }
                            return result;
                        });
                     */
                }
                else
                {
                    return app(env);
                }
            };
        }
    }
}
