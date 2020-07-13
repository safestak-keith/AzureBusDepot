using System;
using System.Collections.Generic;

namespace AzureBusDepot.Abstractions
{
    public class OutboundMessage<TMessage> where TMessage : class
    {
        public TMessage Payload { get; }
        public IDictionary<string, object> UserProperties { get; }
        public string MessageId { get; set; }
        public string PartitionKey { get; set; }
        public string ViaPartitionKey { get; set; }
        public string SessionId { get; set; }
        public string ReplyToSessionId { get; set; }
        public TimeSpan? TimeToLive { get; set; }
        public string CorrelationId { get; set; }
        public string Label { get; set; }
        public string ContentType { get; set; }
        public string ReplyTo { get; set; }
        public DateTime? ScheduledEnqueueTimeUtc { get; set; }

        public OutboundMessage(TMessage payload, IDictionary<string, object> userProperties = null)
        {
            Payload = payload;
            UserProperties = userProperties ?? new Dictionary<string, object>();
        }

        public static OutboundMessage<T> FromEntity<T>(T entity, IDictionary<string, object> userProperties = null) 
            where T : class
        {
            return new OutboundMessage<T>(entity, userProperties);
        }
    }
}