using System;
using AzureBusDepot.Abstractions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AzureBusDepot.ApplicationInsights
{
    public static class ServiceCollectionAppInsightsExtensions
    {
        public static IServiceCollection AddApplicationInsights(this IServiceCollection services, ApplicationInsightsConfig config)
        {
            services.TryAddSingleton(config);
            services.TryAddSingleton(CreateTelemetryClient);
            services.TryAddSingleton<IInstrumentor, ApplicationInsightsInstrumentor>();

            return services;
        }

        private static TelemetryClient CreateTelemetryClient(IServiceProvider s)
        {
            var config = TelemetryConfiguration.Active;
            config.InstrumentationKey = s.GetService<ApplicationInsightsConfig>()?.InstrumentationKey;

            return new TelemetryClient(config);
        }
    }
}
