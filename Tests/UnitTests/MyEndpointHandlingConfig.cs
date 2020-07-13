using AzureBusDepot.Abstractions;

namespace AzureBusDepot.UnitTests
{
    public class MyEndpointHandlingConfig : IEndpointHandlingConfig
    {
        public string ConnectionString { get; set; }

        public string EntityPath { get; set; }

        public bool AutoComplete { get; set; }

        public int MaxConcurrentCalls { get; set; }

        public int MaxAutoRenewDurationSeconds { get; set; }

        public bool DeadLetterOnUnhandledException { get; set; }

        public static readonly MyEndpointHandlingConfig Default = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.multimessagetype/Subscriptions/MultiMessageTypeListenerShould",
            MaxConcurrentCalls = 2,
            MaxAutoRenewDurationSeconds = 60,
            AutoComplete = false,
        };
    }
}
