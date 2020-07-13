using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureBusDepot.Abstractions
{
    public interface IMessageSendingGateway
    {
        Task SendAsync<TMessage>(TMessage messageEntity, IDictionary<string, object> userProperties = null)
            where TMessage : class;

        Task SendMultipleAsync<TMessage>(IEnumerable<TMessage> messageEntities, IDictionary<string, object> userProperties = null) 
            where TMessage : class;

        Task SendAsync<TMessage>(OutboundMessage<TMessage> message)
            where TMessage : class;

        Task SendMultipleAsync<TMessage>(IEnumerable<OutboundMessage<TMessage>> messageEntities)
            where TMessage : class;
    }

    public interface IMessageSendingGateway<TConfig> : IMessageSendingGateway where TConfig : class, IEndpointConfig { }
}
