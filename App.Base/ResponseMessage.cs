using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace App.Common
{
    public class ResponseMessage : IResponseMessage
    {
        public WebSocketMessageType MessageType { get; }
        public byte[] BinaryData { get; }
        public string TextData { get; }
        private ResponseMessage(byte[] binary, string text, WebSocketMessageType messageType)
        {
            BinaryData = binary;
            TextData = text;
            MessageType = messageType;
        }
        public static ResponseMessage New(string data)
        {
            return new ResponseMessage(null, data, WebSocketMessageType.Text);
        }
        public static ResponseMessage New(byte[] data)
        {
            return new ResponseMessage(data, null, WebSocketMessageType.Text);
        }
    }
}
