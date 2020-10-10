using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace App.Common
{
    public interface IResponseMessage
    {
        WebSocketMessageType MessageType { get; }
        byte[] BinaryData { get; }
        string TextData { get; }
    }
}
