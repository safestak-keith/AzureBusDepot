using System;
using System.Collections.Generic;
using System.Text;

namespace AzureBusDepot.Exceptions
{
    public class MessageSerialisationException : Exception
    {
        public MessageSerialisationException()
        { 
        }

        public MessageSerialisationException(string message) : base(message)
        {
        }

        public MessageSerialisationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
