using System;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace mRPC
{
    /// <summary>
    /// mRPC Middleware using <see cref="WebSocket"/>
    /// </summary>
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebSocketMiddleware> _logger;
        private readonly RPCServer _server;

        public WebSocketMiddleware(
            RequestDelegate next,
            ILogger<WebSocketMiddleware> logger,
            RPCServer server)
        {
            _next = next;
            _logger = logger;
            _server = server;
        }

        // result of reading from a WebSocket
        private class WebSocketReadResult
        {
            public WebSocketMessageType Type { get; set; }
            public JObject JSON { get; set; }
            public byte[] Data { get; set; }
        }

        // called by ASP.NET for every request
        public async Task Invoke(HttpContext context)
        {
            // see if we want to handle this request
            if (!context.WebSockets.IsWebSocketRequest
                || context.Request.Path != "/mRPC/")
            {
                await _next.Invoke(context);
                return;
            }

            // connect socket and pass off to RPC server
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connection = new WebsocketConnection(context, socket);
            await _server.HandleConnection(connection);
        }
    }
}
