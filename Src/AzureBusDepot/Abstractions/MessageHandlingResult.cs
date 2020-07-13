using System;
using System.Collections.Generic;

namespace AzureBusDepot.Abstractions
{
    public class MessageHandlingResult
    {
        public enum HandlingResult
        {
            UnrecognisedMessageType,
            Completed,
            DeadLettered,
            Abandoned,
        }

        public HandlingResult Result { get; }
        
        public IDictionary<string, object> AdditionalProperties { get; }

        public Exception Exception { get; }

        public MessageHandlingResult(
            HandlingResult result,
            IDictionary<string, object> additionalProperties = null,
            Exception ex = null)
        {
            Result = result;
            AdditionalProperties = additionalProperties;
            Exception = ex;
        }

        public static MessageHandlingResult UnrecognisedMessageType(
            string details,
            IDictionary<string, object> additionalProperties = null)
        {
            var properties = additionalProperties ?? new Dictionary<string, object>();

            SetOrOverrideProperty(properties, "AzureBusDepot.UnrecognisedMessageType", details);

            return new MessageHandlingResult(
                HandlingResult.UnrecognisedMessageType,
                properties);
        }

        public static MessageHandlingResult Completed(
            IDictionary<string, object> additionalProperties = null)
        {
            return new MessageHandlingResult(
                HandlingResult.Completed,
                additionalProperties ?? new Dictionary<string, object>());
        }

        public static MessageHandlingResult DeadLettered(
            string details,
            IDictionary<string, object> additionalProperties = null)
        {
            var properties = additionalProperties ?? new Dictionary<string, object>();

            SetOrOverrideProperty(properties, "AzureBusDepot.DeadLettered", details);

            return new MessageHandlingResult(
                HandlingResult.DeadLettered,
                properties);
        }

        public static MessageHandlingResult DeadLettered(
            Exception ex,
            IDictionary<string, object> additionalProperties = null)
        {
            var properties = additionalProperties ?? new Dictionary<string, object>();
            SetOrOverrideProperty(properties, "AzureBusDepot.DeadLettered", "Exception");
            SetOrOverrideProperty(properties, "AzureBusDepot.Exception.Message", ex.Message);
            SetOrOverrideProperty(properties, "AzureBusDepot.Exception.StackTrace", ex.StackTrace);

            return new MessageHandlingResult(
                HandlingResult.DeadLettered,
                properties,
                ex);
        }

        public static MessageHandlingResult Abandoned(
            string details,
            IDictionary<string, object> additionalProperties = null)
        {
            var properties = additionalProperties ?? new Dictionary<string, object>();

            SetOrOverrideProperty(properties, "AzureBusDepot.Abandoned", details);

            return new MessageHandlingResult(
                HandlingResult.Abandoned,
                properties);
        }

        public static MessageHandlingResult Abandoned(
            Exception ex,
            IDictionary<string, object> additionalProperties = null)
        {
            var properties = additionalProperties ?? new Dictionary<string, object>();

            SetOrOverrideProperty(properties, "AzureBusDepot.Abandoned", "Exception");
            SetOrOverrideProperty(properties, "AzureBusDepot.Exception.Message", ex.Message);
            SetOrOverrideProperty(properties, "AzureBusDepot.Exception.StackTrace", ex.StackTrace);

            return new MessageHandlingResult(
                HandlingResult.Abandoned,
                properties,
                ex);
        }

        private static void SetOrOverrideProperty(IDictionary<string, object> properties, string key, string details)
        {
            if (properties.ContainsKey(key))
            {
                properties[key] = details;
            }
            else
            {
                properties.Add(key, details);
            }
        }
    }
}
