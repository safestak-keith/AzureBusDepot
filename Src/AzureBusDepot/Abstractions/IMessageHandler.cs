using System.Threading;
using System.Threading.Tasks;

namespace AzureBusDepot.Abstractions
{
    public interface IMessageHandler<TMessage> where TMessage : class
    {
        Task<MessageHandlingResult> HandleMessageAsync(
            TMessage message, MessageContext context, CancellationToken cancellationToken);
    }
}
