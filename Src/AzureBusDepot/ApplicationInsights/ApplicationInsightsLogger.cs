using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace AzureBusDepot.ApplicationInsights
{
    internal class ApplicationInsightsLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsLogger(string name, TelemetryClient telemetryClient)
        {
            _categoryName = name;
            _telemetryClient = telemetryClient;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Trace;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (this.IsEnabled(logLevel))
            {
                if (exception != null)
                    LogAsException(logLevel, eventId, state, exception, formatter);
                else
                    LogAsTrace(logLevel, eventId, state, formatter);
            }
        }

        private void LogAsException<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var properties = new Dictionary<string, string>();
            AddCustomProperties(state, eventId, properties);

            _telemetryClient.TrackException(exception, properties);
        }

        private void LogAsTrace<TState>(LogLevel logLevel, EventId eventId, TState state, Func<TState, Exception, string> formatter)
        {
            var traceTelemetry = new TraceTelemetry(formatter(state, null), GetSeverityLevel(logLevel));
            var dict = traceTelemetry.Context.GlobalProperties;
            AddCustomProperties(state, eventId, dict);

            _telemetryClient.TrackTrace(traceTelemetry);
        }

        private void AddCustomProperties<TState>(TState state, EventId eventId, IDictionary<string, string> dict)
        {
            dict["CategoryName"] = this._categoryName;

            if (!dict.ContainsKey("EventId"))
            {
                dict.Add("EventId", eventId.ToString());
            }

            if (state is IReadOnlyList<KeyValuePair<string, object>> stateDictionary)
            {
                foreach (var item in stateDictionary)
                {
                    dict[item.Key] = Convert.ToString(item.Value);
                }
            }
        }

        private static SeverityLevel GetSeverityLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return SeverityLevel.Critical;
                case LogLevel.Error:
                    return SeverityLevel.Error;
                case LogLevel.Warning:
                    return SeverityLevel.Warning;
                case LogLevel.Information:
                    return SeverityLevel.Information;
                case LogLevel.Debug:
                case LogLevel.Trace:
                default:
                    return SeverityLevel.Verbose;
            }
        }
    }
}
