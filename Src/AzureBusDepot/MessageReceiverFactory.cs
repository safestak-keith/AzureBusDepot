using System;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace AzureBusDepot
{
    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        public static readonly RetryExponential DefaultRetryPolicy = new RetryExponential(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(30), 5);

        public IMessageReceiver CreateMessageReceiver(IEndpointHandlingConfig config, RetryPolicy policy = null)
        {
            return CreateMessageReceiver(config.ConnectionString, config.EntityPath, retryPolicy: policy ?? DefaultRetryPolicy);
        }

        public IMessageReceiver CreateDeadLetterMessageReceiver(IEndpointHandlingConfig config, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy retryPolicy = null, int prefetchCount = 0)
        {
            return CreateDeadLetterMessageReceiver(config.ConnectionString, config.EntityPath, receiveMode, retryPolicy ?? DefaultRetryPolicy, prefetchCount);
        }

        public IMessageReceiver CreateDeadLetterMessageReceiver(string connectionString, string entityPath, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy retryPolicy = null, int prefetchCount = 0)
        {
            var deadLetterPath = EntityNameHelper.FormatDeadLetterPath(entityPath);

            return CreateMessageReceiver(connectionString, deadLetterPath, receiveMode, retryPolicy ?? DefaultRetryPolicy, prefetchCount);
        }

        public IMessageReceiver CreateMessageReceiver(string connectionString, string entityPath, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy retryPolicy = null, int prefetchCount = 0)
        {
            return new MessageReceiver(connectionString, entityPath, receiveMode, retryPolicy ?? DefaultRetryPolicy, prefetchCount);
        }
    }
}
