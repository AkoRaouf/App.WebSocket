using System;
using System.Collections.Generic;
using System.Text;

namespace App.Client.Exception
{
    public class WebsocketBadInputException : WebsocketException
    {
        /// <inheritdoc />
        public WebsocketBadInputException()
        {
        }

        /// <inheritdoc />
        public WebsocketBadInputException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public WebsocketBadInputException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
