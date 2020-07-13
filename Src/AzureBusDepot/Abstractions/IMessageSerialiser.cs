using Microsoft.Azure.ServiceBus;

namespace AzureBusDepot.Abstractions
{
    public interface IMessageSerialiser
    {
        byte[] Serialise<T>(T contractMessage) where T : class;

        T Deserialise<T>(Message message) where T : class;
    }
}