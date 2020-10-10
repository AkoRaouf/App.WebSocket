using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace App.Common
{
    public class ResponseMessage
    {
        private ResponseMessage(byte[] binary, string text, WebSocketMessageType messageType)
        {
            BinaryData = binary;
            TextData = text;
            MessageType = messageType;
        }

        /// <summary>
        /// Received text message (only if type = WebSocketMessageType.Text)
        /// </summary>
        public string TextData { get; }

        /// <summary>
        /// Received text message (only if type = WebSocketMessageType.Binary)
        /// </summary>
        public byte[] BinaryData { get; }

        /// <summary>
        /// Current message type (Text or Binary)
        /// </summary>
        public WebSocketMessageType MessageType { get; }

        /// <summary>
        /// Return string info about the message
        /// </summary>
        public override string ToString()
        {
            if (MessageType == WebSocketMessageType.Text)
            {
                return TextData;
            }

            return $"Type binary, length: {BinaryData?.Length}";
        }

        /// <summary>
        /// Create text response message
        /// </summary>
        public static ResponseMessage New(string data)
        {
            return new ResponseMessage(null, data, WebSocketMessageType.Text);
        }

        /// <summary>
        /// Create binary response message
        /// </summary>
        public static ResponseMessage New(byte[] data)
        {
            return new ResponseMessage(data, null, WebSocketMessageType.Binary);
        }
    }
}
