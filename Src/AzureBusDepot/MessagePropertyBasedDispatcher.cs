using System;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot
{
    public class MessagePropertyBasedDispatcher : IMessageProcessorDispatcher
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _messagePropertyName;

        public MessagePropertyBasedDispatcher(
            ILogger<MessagePropertyBasedDispatcher> logger,
            IServiceProvider serviceProvider,
            string messagePropertyName)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _messagePropertyName = messagePropertyName
                ?? throw new ArgumentNullException(nameof(messagePropertyName));
        }

        public IMessageProcessor GetProcessorForMessage(Message message)
        {
            if (!message.UserProperties.ContainsKey(_messagePropertyName))
            {
                _logger.LogError(LogEventIds.ProcessorDispatcherMissingType, $"Message does not contain property {_messagePropertyName}", _messagePropertyName);
                return null;
            }

            var fullyQualifiedMessageType = message.UserProperties[_messagePropertyName] as string;
            if (string.IsNullOrEmpty(fullyQualifiedMessageType))
            {
                _logger.LogError(LogEventIds.ProcessorDispatcherMissingType, $"No type information found for message in property {_messagePropertyName}", _messagePropertyName);
                return null;
            }

            var processorType = typeof(IMessageProcessor<>);
            var messageType = Type.GetType(fullyQualifiedMessageType);
            if (messageType == null)
            {
                _logger.LogError(LogEventIds.ProcessorDispatcherMissingType, $"No type found in assemblies matching type {_messagePropertyName}", _messagePropertyName);
                return null;
            }

            return _serviceProvider.GetService(processorType.MakeGenericType(messageType)) as IMessageProcessor;
        }
    }
}