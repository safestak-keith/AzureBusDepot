using AzureBusDepot.Abstractions;

namespace AzureBusDepot.UnitTests
{
    public class MyEndpointConfig : IEndpointConfig
    {
        public string ConnectionString { get; set; }

        public string EntityPath { get; set; }
    }
}
