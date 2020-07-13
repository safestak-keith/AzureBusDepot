namespace AzureBusDepot.Abstractions
{
    public interface IEndpointHandlingConfig : IEndpointConfig
    {
        bool AutoComplete { get; set; }

        int MaxConcurrentCalls { get; set; }

        int MaxAutoRenewDurationSeconds { get; set; }
    }
}