using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.ApplicationInsights
{
    internal class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsLoggerProvider(TelemetryClient telemetryClient)
        {
            this._telemetryClient = telemetryClient;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(categoryName, this._telemetryClient) ;
        }

        public void Dispose()
        {
        }
    }
}
