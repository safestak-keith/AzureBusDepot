using AzureBusDepot.Abstractions;

namespace AzureBusDepot.Samples.NetCoreConsoleApp.MessageSending
{
    public class SingleMessageTypeEndpointConfig : IEndpointConfig
    {
        public string ConnectionString { get; set; }
        public string EntityPath { get; set; }
    }
}
