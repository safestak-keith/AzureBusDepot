using Microsoft.Azure.ServiceBus;

namespace AzureBusDepot.Abstractions
{
    public interface IMessageProcessorDispatcher
    {
        IMessageProcessor GetProcessorForMessage(Message message);
    }
}
