using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace AzureBusDepot.UnitTests
{
    public class SpyLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel level, EventId eventId, Exception ex)> _recordedEntries = new List<(LogLevel level, EventId eventId, Exception ex)>();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _recordedEntries.Add((logLevel, eventId, exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool HasExceptionBeenLogged(LogLevel level, EventId eventId, Exception ex)
        {
            return _recordedEntries.Any(
                l => l.level == level && l.eventId == eventId && l.ex.GetType() == ex.GetType());
        }
    }
}
