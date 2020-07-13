using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.ApplicationInsights
{
    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder AddApplicationInsights(this ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.Services.AddSingleton<ILoggerProvider, ApplicationInsightsLoggerProvider>();
            return loggingBuilder;
        }
    }
}
