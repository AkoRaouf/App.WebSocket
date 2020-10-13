using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace App.Server.Web
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await SendResponse(webSocket,
                            ResponseMessage.New($"Hello, you are connected to SimpleStartup"));
                        await HandleRequest(webSocket, context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });
        }

        protected virtual async Task HandleRequest(WebSocket webSocket, HttpContext context)
        {
            while (true)
            {
                var request = await ReadRequest(webSocket);
                var result = request.result;
                if (result.CloseStatus.HasValue)
                {
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    return;
                }

                if (request.message.MessageType == WebSocketMessageType.Text)
                    await HandleTextRequest(webSocket, context, request.message);

                if (request.message.MessageType == WebSocketMessageType.Binary)
                    await HandleBinaryRequest(webSocket, context, request.message);
            }
        }

        protected virtual Task HandleTextRequest(WebSocket webSocket, HttpContext context, ResponseMessage request)
        {
            var msg = (request.TextData ?? string.Empty).Trim().ToLower();

            switch (msg)
            {
                case "ping":
                    return SendResponse(webSocket, ResponseMessage.New("pong"));
                case "Hello":
                    return SendResponse(webSocket, ResponseMessage.New("Hi"));
                case "Bye":
                    return webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
            }

            throw new NotSupportedException($"Request: '{msg}' is not supported");
        }

        protected virtual Task HandleBinaryRequest(WebSocket webSocket, HttpContext context, ResponseMessage request)
        {
            return SendEcho(webSocket, request.BinaryData);
        }

        protected virtual async Task<(WebSocketReceiveResult result, ResponseMessage message)> ReadRequest(WebSocket webSocket)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);

            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.CloseStatus.HasValue)
                        return (result, null);

                    if (buffer.Array != null)
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                ResponseMessage message;
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var data = GetEncoding().GetString(ms.ToArray());
                    message = ResponseMessage.New(data);
                }
                else
                {
                    var data = ms.ToArray();
                    message = ResponseMessage.New(data);
                }

                return (result, message);
            }
        }

        protected virtual async Task SendResponse(WebSocket webSocket, ResponseMessage message)
        {
            if (message.MessageType == WebSocketMessageType.Binary)
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(message.BinaryData, 0, message.BinaryData.Length),
                    message.MessageType,
                    true,
                    CancellationToken.None);
                return;
            }

            if (message.MessageType == WebSocketMessageType.Text)
            {
                var encoding = GetEncoding();
                var bytes = encoding.GetBytes(message.TextData);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes, 0, bytes.Length),
                    message.MessageType,
                    true,
                    CancellationToken.None);
                return;
            }
        }

        protected virtual Encoding GetEncoding()
        {
            return Encoding.UTF8;
        }

        private async Task SendEcho(WebSocket webSocket, string msg, bool slowdown)
        {
            if (slowdown)
                await Task.Delay(100);
            await SendResponse(webSocket, ResponseMessage.New(msg));
        }

        private async Task SendEcho(WebSocket webSocket, byte[] msg)
        {
            await SendResponse(webSocket, ResponseMessage.New(msg));
        }
    }
}
