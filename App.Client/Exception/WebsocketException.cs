using System;

namespace App.Client.Exception
{
    public class WebsocketException : System.Exception
    {
        /// <inheritdoc />
        public WebsocketException()
        {
        }

        /// <inheritdoc />
        public WebsocketException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public WebsocketException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
