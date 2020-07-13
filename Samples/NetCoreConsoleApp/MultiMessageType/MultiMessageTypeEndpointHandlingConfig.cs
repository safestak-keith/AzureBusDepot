using AzureBusDepot.Abstractions;

namespace AzureBusDepot.Samples.NetCoreConsoleApp.MultiMessageType
{
    public class MultiMessageTypeEndpointHandlingConfig : IEndpointHandlingConfig
    {
        public string ConnectionString { get; set; }

        public string EntityPath { get; set; }

        public bool AutoComplete { get; set; }

        public int MaxConcurrentCalls { get; set; }

        public int MaxAutoRenewDurationSeconds { get; set; }

        public bool DeadLetterOnUnhandledException { get; set; }
    }
}
