using System;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot
{
    public abstract class MessageListener<TConfig>
        where TConfig : class, IEndpointHandlingConfig
    {
        protected readonly ILogger _logger;
        protected readonly IMessageReceiver _receiver;
        protected readonly IInstrumentor _instrumentor;
        protected readonly TConfig _endpointHandlingConfig;
        protected readonly string _name;

        protected MessageListener(
            ILogger logger,
            IMessageReceiverFactory messageReceiverFactory,
            IInstrumentor instrumentor,
            TConfig endpointHandlingConfig,
            string name)
        {
            _logger = logger;
            _instrumentor = instrumentor;
            _receiver = messageReceiverFactory.CreateMessageReceiver(endpointHandlingConfig);
            _endpointHandlingConfig = endpointHandlingConfig;
            _name = name;
        }

        protected Task RegisterHandler(
            Func<Message, CancellationToken, Task> handler, 
            CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return Task.FromCanceled(ct);

            _logger.LogInformation(
                LogEventIds.ListenerStarted,
                $"Started listening for {_name} on {_receiver.ServiceBusConnection.Endpoint}/{_endpointHandlingConfig.EntityPath}",
                _receiver.ServiceBusConnection.Endpoint,
                _endpointHandlingConfig.EntityPath,
                _name);

            var messageHandlerOptions = new MessageHandlerOptions(HandleException)
            {
                AutoComplete = _endpointHandlingConfig.AutoComplete,
                MaxConcurrentCalls = _endpointHandlingConfig.MaxConcurrentCalls,
                MaxAutoRenewDuration = TimeSpan.FromSeconds(_endpointHandlingConfig.MaxAutoRenewDurationSeconds)
            };
            _receiver.RegisterMessageHandler(handler, messageHandlerOptions);

            return Task.CompletedTask;
        }

        protected async Task CloseReceiverAsync()
        {
            if (!_receiver.IsClosedOrClosing)
            {
                await _receiver.CloseAsync().ConfigureAwait(false);
                _logger.LogInformation(
                    LogEventIds.ListenerFinished,
                    $"Stopped listening for {_name} on {_receiver.ServiceBusConnection.Endpoint}/{_endpointHandlingConfig.EntityPath}",
                    _receiver.ServiceBusConnection.Endpoint, _name);
            }
        }

        protected async Task<bool> HandleMessageOutcome(
            Message message, 
            MessageHandlingResult result)
        {
            var isSuccessful = result.Result == MessageHandlingResult.HandlingResult.Completed;
            var shouldAutoComplete = _endpointHandlingConfig.AutoComplete || _receiver.IsClosedOrClosing;
            if (shouldAutoComplete)
                return isSuccessful;

            try
            {
                switch (result.Result)
                {
                    case MessageHandlingResult.HandlingResult.Completed:
                        await _receiver.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
                        break;
                    case MessageHandlingResult.HandlingResult.DeadLettered:
                    case MessageHandlingResult.HandlingResult.UnrecognisedMessageType:
                        await _receiver.DeadLetterAsync(message.SystemProperties.LockToken, result.AdditionalProperties).ConfigureAwait(false);
                        break;
                    case MessageHandlingResult.HandlingResult.Abandoned:
                        await _receiver.AbandonAsync(message.SystemProperties.LockToken, result.AdditionalProperties).ConfigureAwait(false);
                        break;
                }
            }
            catch (MessageLockLostException ex)
            {
                _logger.LogError(LogEventIds.ListenerException, ex, $"MessageLockLostException in {_name}>");
            }
            return isSuccessful;
        }

        protected Task HandleException(
            ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            if (exceptionReceivedEventArgs.Exception is OperationCanceledException)
            {
                _logger.LogWarning(
                    LogEventIds.ListenerCancelled,
                    exceptionReceivedEventArgs.Exception,
                    $"Operation was cancelled in {_name}:{exceptionReceivedEventArgs.Exception.Message}",
                    exceptionReceivedEventArgs.ExceptionReceivedContext);
                return Task.CompletedTask;
            }

            _logger.LogError(
                LogEventIds.ListenerException,
                exceptionReceivedEventArgs.Exception,
                $"Message Handling Exception in {_name}:{exceptionReceivedEventArgs.Exception.Message}",
                exceptionReceivedEventArgs.ExceptionReceivedContext);

            return Task.CompletedTask;
        }
    }
}