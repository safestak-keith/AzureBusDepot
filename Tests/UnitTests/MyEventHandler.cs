using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;

namespace AzureBusDepot.UnitTests
{
    public class MyEventHandler : IMessageHandler<MyEvent>
    {
        public Task<MessageHandlingResult> HandleMessageAsync(
            MyEvent message, MessageContext context, CancellationToken ct)
        {
            return Task.FromResult(MessageHandlingResult.Completed());
        }
    }
}
