using System.Threading;
using System.Threading.Tasks;

namespace AzureBusDepot.Abstractions
{
    public interface IMessageListener
    {
        Task StartListeningAsync(CancellationToken ct);

        Task StopListeningAsync();
    }

    public interface IMessageListener<TConfig> : IMessageListener where TConfig : class, IEndpointHandlingConfig { }
}
