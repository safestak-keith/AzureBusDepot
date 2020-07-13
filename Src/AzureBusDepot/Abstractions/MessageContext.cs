using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;

namespace AzureBusDepot.Abstractions
{
    public class MessageContext
    {
        public IDictionary<string, object> UserProperties { get; }
        public DateTime ScheduledEnqueueTimeUtc { get; }
        public string Label { get; }
        public string CorrelationId { get; }
        public string MessageId { get; }
        public DateTime ExpiresAtUtc { get; }
        public int DeliveryCount { get; }
        public long SequenceNumber { get; }
        public DateTime EnqueuedTimeUtc { get; }

        public MessageContext(
            IDictionary<string, object> userProperties,
            DateTime scheduledEnqueueTimeUtc,
            string label,
            string correlationId,
            string messageId,
            DateTime expiresAtUtc,
            int deliveryCount,
            long sequenceNumber,
            DateTime enqueuedTimeUtc)
        {
            UserProperties = userProperties;
            ScheduledEnqueueTimeUtc = scheduledEnqueueTimeUtc;
            Label = label;
            CorrelationId = correlationId;
            MessageId = messageId;
            ExpiresAtUtc = expiresAtUtc;
            DeliveryCount = deliveryCount;
            SequenceNumber = sequenceNumber;
            EnqueuedTimeUtc = enqueuedTimeUtc;
        }

        public static MessageContext Create(Message message)
        {
            return new MessageContext
            (
                message.UserProperties,
                message.ScheduledEnqueueTimeUtc,
                message.Label,
                message.CorrelationId,
                message.MessageId,
                message.ExpiresAtUtc,
                message.SystemProperties.DeliveryCount,
                message.SystemProperties.SequenceNumber,
                message.SystemProperties.EnqueuedTimeUtc
            );
        }
    }
}
