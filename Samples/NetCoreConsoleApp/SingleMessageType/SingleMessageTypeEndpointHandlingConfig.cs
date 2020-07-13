using AzureBusDepot.Abstractions;

namespace AzureBusDepot.Samples.NetCoreConsoleApp.SingleMessageType
{
    public class SingleMessageTypeEndpointHandlingConfig : IEndpointHandlingConfig
    {
        public string ConnectionString { get; set; }

        public string EntityPath { get; set; }

        public bool AutoComplete { get; set; }

        public int MaxConcurrentCalls { get; set; }

        public int MaxAutoRenewDurationSeconds { get; set; }

        public bool DeadLetterOnUnhandledException { get; set; }
    }
}
