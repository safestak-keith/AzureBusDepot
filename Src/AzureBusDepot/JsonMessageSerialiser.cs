using System;
using System.Runtime.Serialization;
using System.Text;
using AzureBusDepot.Abstractions;
using AzureBusDepot.Exceptions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.InteropExtensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureBusDepot
{
    public class JsonMessageSerialiser : IMessageSerialiser
    {
        private readonly ILogger _logger;

        public JsonMessageSerialiser(ILogger<JsonMessageSerialiser> logger)
        {
            _logger = logger;
        }

        public byte[] Serialise<T>(T contractMessage) where T : class
        {
            if (contractMessage == null) throw new ArgumentNullException(nameof(contractMessage));

            try
            {
                var json = JsonConvert.SerializeObject(contractMessage);

                return Encoding.UTF8.GetBytes(json);
            }
            catch (JsonException ex)
            {
                _logger.LogError(LogEventIds.SerialiserException, ex, $"Unhandled exception serialising {typeof(T).Name}", ex, contractMessage);
                throw new MessageSerialisationException($"Unable to serialise {typeof(T).Name}", ex);
            }
        }

        public T Deserialise<T>(Message message) where T : class
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            try
            {
                // support for messages that are sent using Value types and not a byte[]
                if (message.Body == null)
                {
                    var messageBody = message.GetBody<string>();
                    if (string.IsNullOrWhiteSpace(messageBody))
                        throw new MessageSerialisationException($"Unable to deserialise message {message.MessageId} as the message has an empty Body property.");
                    return JsonConvert.DeserializeObject<T>(messageBody);
                }

                var json = Encoding.UTF8.GetString(message.Body);
                if (string.IsNullOrWhiteSpace(json))
                    throw new MessageSerialisationException($"Unable to deserialise message {message.MessageId} as it is a null or whitespace string.");

                // fallback support for messages that are sent using older SDKs or use an XML serialiser
                if (json[0] != '{')
                    json = message.GetBody<string>();

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogError(LogEventIds.SerialiserException, ex, $"Unhandled exception deserialising {typeof(T).Name}", ex, message);
                throw new MessageSerialisationException($"Unable to deserialise {typeof(T).Name}", ex);
            }
            catch (SerializationException ex)
            {
                _logger.LogError(LogEventIds.SerialiserException, ex, $"Unhandled exception deserialising {typeof(T).Name}", ex, message);
                throw new MessageSerialisationException($"Unable to deserialise {typeof(T).Name}", ex);
            }
        }
    }
}
