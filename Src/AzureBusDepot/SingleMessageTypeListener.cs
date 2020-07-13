using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot
{
    public class SingleMessageTypeListener<TConfig, TMessage> : MessageListener<TConfig>, IMessageListener<TConfig>
        where TConfig : class, IEndpointHandlingConfig
        where TMessage : class
    {
        private readonly IMessageProcessor<TMessage> _processor;

        public SingleMessageTypeListener(
            ILogger<SingleMessageTypeListener<TConfig, TMessage>> logger,
            IMessageProcessor<TMessage> processor,
            IMessageReceiverFactory messageReceiverFactory,
            IInstrumentor instrumentor,
            TConfig endpointHandlingConfig) 
            : base(logger, messageReceiverFactory,instrumentor,endpointHandlingConfig, $"{nameof(SingleMessageTypeListener<TConfig, TMessage>)}<{typeof(TConfig).Name},{typeof(TMessage).Name}>")
        {
            _processor = processor;
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
            var sw = Stopwatch.StartNew();
            var timestamp = DateTimeOffset.UtcNow;

            var processingResult = await _processor.ProcessMessageAsync(message, _endpointHandlingConfig, ct).ConfigureAwait(false);
            var isSuccessful = await HandleMessageOutcome(message, processingResult).ConfigureAwait(false);

            _instrumentor.TrackRequest(
                LogEventIds.ListenerHandlerFinished, 
                sw.ElapsedMilliseconds, 
                timestamp, 
                _name, 
                isSuccessful:isSuccessful, 
                customProperties:processingResult.AdditionalProperties);
        }
    }
}
