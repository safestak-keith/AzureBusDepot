using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot
{
    public class MultiMessageTypeListener<TConfig> : MessageListener<TConfig>, IMessageListener<TConfig>
        where TConfig : class, IEndpointHandlingConfig
    {
        private readonly IMessageProcessorDispatcher _processorDispatcher;

        public MultiMessageTypeListener(
            ILogger<MultiMessageTypeListener<TConfig>> logger,
            IMessageProcessorDispatcher processorDispatcher,
            IMessageReceiverFactory messageReceiverFactory,
            IInstrumentor instrumentor,
            TConfig endpointHandlingConfig)
            : base(logger, messageReceiverFactory, instrumentor, endpointHandlingConfig, $"{nameof(MultiMessageTypeListener<TConfig>)}<{typeof(TConfig).Name}>")
        {
            _processorDispatcher = processorDispatcher;
        }

        public Task StartListeningAsync(CancellationToken ct)
        {
            return RegisterHandler(HandleMessage, ct);
        }

        public Task StopListeningAsync()
        {
            return CloseReceiverAsync();
        }

        private async Task HandleMessage(Message message, CancellationToken ct)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var sw = Stopwatch.StartNew();

            var processor = _processorDispatcher.GetProcessorForMessage(message);
            if (processor == null)
            {
                _logger.LogError(LogEventIds.ProcessorDispatcherMissingType, "Unmapped MessageProcessor<T> for input message", message);
                if (!_endpointHandlingConfig.AutoComplete && !_receiver.IsClosedOrClosing)
                    await _receiver.DeadLetterAsync(message.SystemProperties.LockToken, "Unmapped MessageProcessor<T>").ConfigureAwait(false);
                return;
            }

            var processingResult = await processor.ProcessMessageAsync(message, _endpointHandlingConfig, ct).ConfigureAwait(false);
            var isSuccessful = await HandleMessageOutcome(message, processingResult).ConfigureAwait(false);

            _instrumentor.TrackRequest(
                LogEventIds.ListenerHandlerFinished, 
                sw.ElapsedMilliseconds, 
                timestamp, 
                _name, 
                isSuccessful: isSuccessful, 
                customProperties: processingResult.AdditionalProperties);
        }
    }
}
