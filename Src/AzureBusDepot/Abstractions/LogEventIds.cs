namespace AzureBusDepot.Abstractions
{
    /// <summary>
    /// Used as EventIds for Microsoft.Extensions.Logging.ILogger
    /// </summary>
    public static class LogEventIds
    {
        public const int GeneralError = 100000;
        public const int GeneralDebug = 100001;
        public const int GeneralInfo = 100002;
        public const int GeneralInstrumentorMeasuredElapsed = 100002;
        public const int HostedServiceStarted = 110001;
        public const int HostedServiceFinished = 110002;
        public const int ListenerException = 120000;
        public const int ListenerStarted = 120001;
        public const int ListenerFinished = 120002;
        public const int ListenerCancelled = 120003;
        public const int ListenerHandlerFinished = 120004;
        public const int ProcessorException = 130000;
        public const int ProcessorStarted = 130001;
        public const int ProcessorFinished = 130002;
        public const int ProcessorCancelled = 130003;
        public const int ProcessorDispatcherMissingType = 131000;
        public const int HandlerException = 140000;
        public const int HandlerStarted = 140001;
        public const int HandlerFinished = 140002;
        public const int HandlerCancelled = 140003;
        public const int HandlerMeasuredElapsed = 140004;
        public const int SerialiserException = 150000;
        public const int OutboundGatewayException = 160000;
        public const int OutboundGatewaySentSingle = 160001;
        public const int OutboundGatewaySentMultiple = 160002;
        public const int OutboundGatewayMeasuredElapsedSingle = 160003;
        public const int OutboundGatewayMeasuredElapsedMultiple = 160004;
    }
}
