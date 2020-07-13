using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace AzureBusDepot.Abstractions
{
    public interface IMessageReceiverFactory
    {
        IMessageReceiver CreateMessageReceiver(string connectionString, string entityPath, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy retryPolicy = null, int prefetchCount = 0);

        IMessageReceiver CreateDeadLetterMessageReceiver(string connectionString, string entityPath, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy retryPolicy = null, int prefetchCount = 0);

        IMessageReceiver CreateMessageReceiver(IEndpointHandlingConfig config, RetryPolicy policy = null);
        IMessageReceiver CreateDeadLetterMessageReceiver(IEndpointHandlingConfig config, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy retryPolicy = null, int prefetchCount = 0);
    }
}
