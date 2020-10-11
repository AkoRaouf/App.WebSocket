using App.Client.Logging;
using App.Client.Model;
using App.Client.Model.Websocket.Client;
using App.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace App.Client
{
    public partial class WebsocketClient
    {
        private async Task Listen(WebSocket client, CancellationToken token)
        {
            System.Exception causedException = null;
            try
            {
                // define buffer here and reuse, to avoid more allocation
                const int chunkSize = 1024 * 4;
                var buffer = new ArraySegment<byte>(new byte[chunkSize]);

                do
                {
                    WebSocketReceiveResult result;
                    byte[] resultArrayWithTrailing = null;
                    var resultArraySize = 0;
                    var isResultArrayCloned = false;
                    MemoryStream ms = null;

                    while (true)
                    {
                        result = await client.ReceiveAsync(buffer, token);
                        var currentChunk = buffer.Array;
                        var currentChunkSize = result.Count;

                        var isFirstChunk = resultArrayWithTrailing == null;
                        if (isFirstChunk)
                        {
                            // first chunk, use buffer as reference, do not allocate anything
                            resultArraySize += currentChunkSize;
                            resultArrayWithTrailing = currentChunk;
                            isResultArrayCloned = false;
                        }
                        else if (currentChunk == null)
                        {
                            // weird chunk, do nothing
                        }
                        else
                        {
                            // received more chunks, lets merge them via memory stream
                            if (ms == null)
                            {
                                // create memory stream and insert first chunk
                                ms = new MemoryStream();
                                ms.Write(resultArrayWithTrailing, 0, resultArraySize);
                            }

                            // insert current chunk
                            ms.Write(currentChunk, buffer.Offset, currentChunkSize);
                        }

                        if (result.EndOfMessage)
                        {
                            break;
                        }

                        if (isResultArrayCloned)
                            continue;

                        // we got more chunks incoming, need to clone first chunk
                        resultArrayWithTrailing = resultArrayWithTrailing?.ToArray();
                        isResultArrayCloned = true;
                    }

                    ms?.Seek(0, SeekOrigin.Begin);

                    ResponseMessage message;
                    if (result.MessageType == WebSocketMessageType.Text && IsTextMessageConversionEnabled)
                    {
                        var data = ms != null ?
                            GetEncoding().GetString(ms.ToArray()) :
                            resultArrayWithTrailing != null ?
                                GetEncoding().GetString(resultArrayWithTrailing, 0, resultArraySize) :
                                null;

                        message = ResponseMessage.New(data);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Logger.Trace(L($"Received close message"));
                        var info = DisconnectionInfo.Create(DisconnectionType.ByServer, client, null);
                        _disconnectedSubject.OnNext(info);

                        if (info.CancelClosing)
                        {
                            // closing canceled, reconnect if enabled
                            if (IsReconnectionEnabled)
                            {
                                throw new OperationCanceledException("Websocket connection was closed by server");
                            }

                            continue;
                        }

                        await StopInternal(client, WebSocketCloseStatus.NormalClosure, "Closing",
                            token, false, true);

                        // reconnect if enabled
                        if (IsReconnectionEnabled && !ShouldIgnoreReconnection(client))
                        {
                            _ = ReconnectSynchronized(ReconnectionType.Lost, false, null);
                        }

                        return;
                    }
                    else
                    {
                        if (ms != null)
                        {
                            message = ResponseMessage.New(ms.ToArray());
                        }
                        else
                        {
                            Array.Resize(ref resultArrayWithTrailing, resultArraySize);
                            message = ResponseMessage.New(resultArrayWithTrailing);
                        }
                    }

                    ms?.Dispose();

                    Logger.Trace(L($"Received:  {message}"));
                    _lastReceivedMsg = DateTime.UtcNow;
                    _messageReceivedSubject.OnNext(message);

                } while (client.State == WebSocketState.Open && !token.IsCancellationRequested);
            }
            catch (TaskCanceledException e)
            {
                // task was canceled, ignore
                causedException = e;
            }
            catch (OperationCanceledException e)
            {
                // operation was canceled, ignore
                causedException = e;
            }
            catch (ObjectDisposedException e)
            {
                // client was disposed, ignore
                causedException = e;
            }
            catch (System.Exception e)
            {
                Logger.Error(e, L($"Error while listening to websocket stream, error: '{e.Message}'"));
                causedException = e;
            }


            if (ShouldIgnoreReconnection(client) || !IsStarted)
            {
                // reconnection already in progress or client stopped/disposed, do nothing
                return;
            }

            // listening thread is lost, we have to reconnect
            _ = ReconnectSynchronized(ReconnectionType.Lost, false, causedException);
        }
    }
}
