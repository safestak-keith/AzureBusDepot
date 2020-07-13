using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot
{
    public class MessageProcessor<TMessage> : IMessageProcessor<TMessage> where TMessage : class
    {
        private readonly ILogger _logger;
        private readonly IMessageSerialiser _serialiser;
        private readonly IMessageHandler<TMessage> _handler;
        private readonly IInstrumentor _instrumentor;

        public MessageProcessor(
            ILogger<MessageProcessor<TMessage>> logger,
            IMessageSerialiser serialiser,
            IMessageHandler<TMessage> handler,
            IInstrumentor instrumentor)
        {
            _logger = logger;
            _serialiser = serialiser;
            _handler = handler;
            _instrumentor = instrumentor;
        }

        public async Task<MessageHandlingResult> ProcessMessageAsync(
            Message message, IEndpointHandlingConfig handlingConfig, CancellationToken ct)
        {
            try
            {
                var messageContext = MessageContext.Create(message);

                var contractMessage = _serialiser.Deserialise<TMessage>(message);
                if (contractMessage == null)
                {
                    return MessageHandlingResult.UnrecognisedMessageType(
                        $"Serialiser could not de-serialise message to {typeof(TMessage).AssemblyQualifiedName}", 
                        message.UserProperties);
                }

                var stopwatch = Stopwatch.StartNew();
                var handlingResult = await _handler.HandleMessageAsync(contractMessage, messageContext, ct).ConfigureAwait(false);
                _instrumentor.TrackElapsed(
                    LogEventIds.HandlerMeasuredElapsed, stopwatch.ElapsedMilliseconds, _handler.GetType().Name, message.UserProperties);

                return handlingResult;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(LogEventIds.ProcessorCancelled, ex, $"Operation was cancelled in Processor<{typeof(TMessage).Name}>");
                return MessageHandlingResult.Abandoned(ex, message.UserProperties);
            }
        }
    }
}
