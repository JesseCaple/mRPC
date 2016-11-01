using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mRPC
{
    /// <summary>
    /// <see cref="IConnection"/> implementation using <see cref="WebSocket"/>
    /// </summary>
    public class WebsocketConnection : IConnection
    {
        public readonly WebSocket _webSocket;
        public HttpContext Context { get; }

        public WebsocketConnection(HttpContext context, WebSocket webSocket)
        {
            Context = context;
            _webSocket = webSocket;
        }

        /// <summary>
        /// Checks if the <see cref="WebSocket"/> is still open
        /// </summary>
        public bool Closed
        {
            get
            {
                return _webSocket.State != WebSocketState.Open;
            }
        }

        /// <summary>
        /// Sends a JSON message to the <see cref="WebSocket"/>
        /// </summary>
        public async Task Send(JObject message)
        {
            var bytes = Encoding.UTF8.GetBytes(message.ToString());
            var buffer = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await _webSocket.SendAsync(buffer,
                WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves a JSON message from the <see cref="WebSocket"/>
        /// </summary>
        public async Task<JObject> Receive()
        {
            var message = new JObject();
            var buffer = new ArraySegment<byte>(new byte[4096]);
            using (var stream = new MemoryStream())
            {
                WebSocketReceiveResult recResult;
                do
                {
                    recResult = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    stream.Write(buffer.Array, buffer.Offset, recResult.Count);
                }
                while (!recResult.EndOfMessage);
                stream.Seek(0, SeekOrigin.Begin);

                if (recResult.MessageType == WebSocketMessageType.Text)
                {
                    var array = stream.ToArray();
                    var asString = Encoding.UTF8.GetString(array, 0, array.Length);
                    message = JObject.Parse(asString);
                }
            }
            return message;
        }
    }

}
