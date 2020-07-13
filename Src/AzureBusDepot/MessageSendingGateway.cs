using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot
{
    public class MessageSendingGateway<TConfig> : IMessageSendingGateway<TConfig>
        where TConfig : class, IEndpointConfig
    {
        public const string AzureServiceBusDependencyType = "AzureServiceBus";
        private const string MessageTypeProperty = "AzureBusDepot.EnclosedMessageType";

        private readonly ILogger _logger;
        private readonly IMessageSerialiser _serialiser;
        private readonly IInstrumentor _instrumentor;
        private readonly IMessageSender _sender;

        public MessageSendingGateway(
            ILogger<MessageSendingGateway<TConfig>> logger,
            IMessageSerialiser serialiser,
            IInstrumentor instrumentor,
            IMessageSenderFactory senderFactory,
            TConfig endpointConfig)
        {
            _logger = logger;
            _serialiser = serialiser;
            _instrumentor = instrumentor;
            _sender = senderFactory.CreateMessageSender(endpointConfig);
        }

        public async Task SendAsync<TMessage>(
            TMessage messageEntity,
            IDictionary<string, object> userProperties = null) where TMessage : class
        {
            var message = GenerateMessage(OutboundMessage<TMessage>.FromEntity(messageEntity, userProperties));
            var sw = Stopwatch.StartNew();
            await _sender.SendAsync(message).ConfigureAwait(false);
            TrackDependency(sw, nameof(SendAsync), userProperties);
            _logger.LogDebug(LogEventIds.OutboundGatewaySentSingle, $"Sent single message of type {typeof(TMessage).Name}");
        }

        public async Task SendMultipleAsync<TMessage>(
            IEnumerable<TMessage> messageEntities,
            IDictionary<string, object> userProperties = null) where TMessage : class
        {
            var messageList = messageEntities
                .Select(m => GenerateMessage(OutboundMessage<TMessage>.FromEntity(m, userProperties)))
                .ToList();
            var sw = Stopwatch.StartNew();
            await _sender.SendAsync(messageList).ConfigureAwait(false);
            TrackDependency(sw, nameof(SendMultipleAsync), userProperties);
            _logger.LogDebug(LogEventIds.OutboundGatewaySentMultiple, $"Sent {messageList.Count} messages of type {typeof(TMessage).Name}");
        }

        public async Task SendAsync<TMessage>(OutboundMessage<TMessage> outboundMessage) where TMessage : class
        {
            var message = GenerateMessage(outboundMessage);
            var sw = Stopwatch.StartNew();
            await _sender.SendAsync(message).ConfigureAwait(false);
            TrackDependency(sw, nameof(SendAsync), outboundMessage.UserProperties);
            _logger.LogDebug(LogEventIds.OutboundGatewaySentSingle, $"Sent single message of {nameof(OutboundMessage<TMessage>)} type {typeof(TMessage).Name}");
        }

        public async Task SendMultipleAsync<TMessage>(IEnumerable<OutboundMessage<TMessage>> outboundMessages) where TMessage : class
        {
            var messageList = outboundMessages
                .Select(GenerateMessage)
                .ToList();
            var sw = Stopwatch.StartNew();
            await _sender.SendAsync(messageList).ConfigureAwait(false);
            TrackDependency(sw, nameof(SendMultipleAsync));
            _logger.LogDebug(LogEventIds.OutboundGatewaySentMultiple, $"Sent {messageList.Count} messages of {nameof(OutboundMessage<TMessage>)} type {typeof(TMessage).Name}");
        }

        private Message GenerateMessage<TMessage>(OutboundMessage<TMessage> outboundMessage) 
            where TMessage : class
        {
            var messageBody = _serialiser.Serialise(outboundMessage.Payload);

            var message = new Message(messageBody)
            {
                CorrelationId = outboundMessage.CorrelationId,
                Label = outboundMessage.Label,
                ContentType = outboundMessage.ContentType,
                ReplyToSessionId = outboundMessage.ReplyToSessionId,
                ReplyTo = outboundMessage.ReplyTo
            };

            if (!string.IsNullOrEmpty(outboundMessage.MessageId))
            {
                message.MessageId = outboundMessage.MessageId;
            }
            if (!string.IsNullOrEmpty(outboundMessage.PartitionKey))
            {
                message.PartitionKey = outboundMessage.PartitionKey;
            }
            if (!string.IsNullOrEmpty(outboundMessage.ViaPartitionKey))
            {
                message.ViaPartitionKey = outboundMessage.ViaPartitionKey;
            }
            if (!string.IsNullOrEmpty(outboundMessage.SessionId))
            {
                message.SessionId = outboundMessage.SessionId;
            }
            if (outboundMessage.TimeToLive.HasValue)
            {
                message.TimeToLive = outboundMessage.TimeToLive.Value;
            }
            if (outboundMessage.ScheduledEnqueueTimeUtc.HasValue)
            {
                message.ScheduledEnqueueTimeUtc = outboundMessage.ScheduledEnqueueTimeUtc.Value;
            }
            if (outboundMessage.UserProperties != null)
            {
                foreach (var kv in outboundMessage.UserProperties)
                {
                    message.UserProperties.Add(kv.Key, kv.Value);
                }
            }
            message.UserProperties.Add(MessageTypeProperty, typeof(TMessage).AssemblyQualifiedName);

            return message;
        }

        private void TrackDependency(
            Stopwatch sw, string name, IDictionary<string, object> customProperties = null) 
        {
            _instrumentor.TrackDependency(
                LogEventIds.OutboundGatewayMeasuredElapsedSingle,
                sw.ElapsedMilliseconds,
                DateTimeOffset.Now, 
                AzureServiceBusDependencyType,
                _sender.Path,
                name,
                isSuccessful: true,
                customProperties: customProperties);
        }
    }
}
