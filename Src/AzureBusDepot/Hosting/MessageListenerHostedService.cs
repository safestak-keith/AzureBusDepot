using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.Hosting
{
    public class MessageListenerHostedService<TConfig> : IHostedService 
        where TConfig : class, IEndpointHandlingConfig
    {
        private readonly ILogger _logger;
        private readonly IMessageListener _listener;

        public MessageListenerHostedService(
            ILogger<MessageListenerHostedService<TConfig>> logger,
            IMessageListener<TConfig> listener)
        {
            _logger = logger;
            _listener = listener;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _logger.LogInformation(
                LogEventIds.HostedServiceStarted, 
                $"Started {nameof(MessageListenerHostedService<TConfig>)} for {typeof(TConfig).Name}");

            await _listener.StartListeningAsync(ct).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                LogEventIds.HostedServiceFinished, 
                $"Stopping {nameof(MessageListenerHostedService<TConfig>)} for {typeof(TConfig).Name}");

            await _listener.StopListeningAsync().ConfigureAwait(false);
        }
    }
}
