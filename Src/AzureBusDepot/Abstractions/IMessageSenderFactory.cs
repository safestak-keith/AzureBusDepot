using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace AzureBusDepot.Abstractions
{
    public interface IMessageSenderFactory
    {
        IMessageSender CreateMessageSender(IEndpointConfig config, RetryPolicy policy = null);
    }
}
