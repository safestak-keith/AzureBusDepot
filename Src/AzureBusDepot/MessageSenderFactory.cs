using System;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace AzureBusDepot
{
    public class MessageSenderFactory : IMessageSenderFactory
    {
        public static readonly RetryExponential DefaultRetryPolicy = new RetryExponential(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(30), 5);

        public IMessageSender CreateMessageSender(IEndpointConfig config, RetryPolicy policy = null)
        {
            return new MessageSender(config.ConnectionString, config.EntityPath, policy ?? DefaultRetryPolicy);
        }
    }
}
