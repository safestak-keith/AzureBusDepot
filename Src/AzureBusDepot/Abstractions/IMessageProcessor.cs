using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace AzureBusDepot.Abstractions
{
    public interface IMessageProcessor
    {
        Task<MessageHandlingResult> ProcessMessageAsync(
            Message message, IEndpointHandlingConfig handlingConfig, CancellationToken ct);
    }

    public interface IMessageProcessor<TMessage> : IMessageProcessor { }
}
