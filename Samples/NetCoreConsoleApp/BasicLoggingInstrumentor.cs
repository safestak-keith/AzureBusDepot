using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AzureBusDepot.Abstractions;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.Samples.NetCoreConsoleApp
{
    /// <summary>
    /// Simple instrumentor which records elapsed times to the ILogger. 
    /// Not used in this sample but can be used in local debugging.
    /// </summary>
    public class BasicLoggingInstrumentor : IInstrumentor
    {
        private readonly ILogger _logger;

        public BasicLoggingInstrumentor(ILogger<BasicLoggingInstrumentor> logger)
        {
            _logger = logger;
        }

        public void TrackElapsed(
            int eventId, long elapsedMilliseconds, [CallerMemberName]string name = "", IDictionary<string, object> customProperties = null)
        {
            _logger.LogInformation(eventId, $"Measured elapsed {name}- {elapsedMilliseconds}ms", elapsedMilliseconds);
        }

        public void TrackRequest(
            int eventId, long elapsedMilliseconds, DateTimeOffset timestamp, string name, string source = null, Uri uri = null, bool isSuccessful = true, IDictionary<string, object> customProperties = null)
        {
            _logger.LogInformation(eventId, $"Measured request {name} from {source}- {elapsedMilliseconds}ms", elapsedMilliseconds);
        }

        public void TrackDependency(
            int eventId, long elapsedMilliseconds, DateTimeOffset timestamp, string type, string target, string name, string data = null, bool isSuccessful = true, IDictionary<string, object> customProperties = null)
        {
            _logger.LogInformation(eventId, $"Measured {type} dependency to {target}/{name}- {elapsedMilliseconds}ms", elapsedMilliseconds);

        }
    }
}
