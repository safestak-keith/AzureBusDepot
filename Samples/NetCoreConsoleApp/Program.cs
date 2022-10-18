using System;
using System.Linq;
using System.Threading.Tasks;
using AzureBusDepot;
using AzureBusDepot.Abstractions;
using AzureBusDepot.ApplicationInsights;
using AzureBusDepot.Hosting;
using AzureBusDepot.Samples.NetCoreConsoleApp.MessageSending;
using AzureBusDepot.Samples.NetCoreConsoleApp.MultiMessageType;
using AzureBusDepot.Samples.NetCoreConsoleApp.SingleMessageType;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.Samples.NetCoreConsoleApp
{
    class Program
    {
        private const string MessageTypeProperty = "AzureBusDepot.EnclosedMessageType";

        static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var appInsightsConfig = hostContext.Configuration
                        .GetSection("ApplicationInsights")
                        .Get<ApplicationInsightsConfig>();

                    // Single message type MyEvent 
                    var topicEndpointConfig = hostContext.Configuration
                        .GetSection("MessageHandlingEndpoints:SingleMessageTypeTopic")
                        .Get<SingleMessageTypeEndpointHandlingConfig>();
                    services.AddAzureBusDepot<JsonMessageSerialiser>()
                        .ConfigureSingleMessageTypeListener<SingleMessageTypeEndpointHandlingConfig, MyEvent, MyEventHandler>(topicEndpointConfig)
                        .AddApplicationInsights(appInsightsConfig);

                    // Multiple message types with dispatcher based on assembly qualified type name in message property "AzureBusDepot.EnclosedMessageType"
                    var queueEndpointConfig = hostContext.Configuration
                        .GetSection("MessageHandlingEndpoints:MultiMessageTypeQueue")
                        .Get<MultiMessageTypeEndpointHandlingConfig>();
                    services.AddAzureBusDepot<JsonMessageSerialiser>()
                        .ConfigureMessageListenerWithPropertyBasedDispatcher(queueEndpointConfig, MessageTypeProperty)
                        .AddMessageHandler<MyFirstCommand, MyFirstCommandHandler>()
                        .AddMessageHandler<MySecondCommand, MySecondCommandHandler>()
                        .AddApplicationInsights(appInsightsConfig);

                    // Message Sending Gateway
                    var topicSendingEndpointConfig = hostContext.Configuration
                        .GetSection("MessageSendingEndpoints:SingleMessageTypeTopic")
                        .Get<SingleMessageTypeEndpointConfig>();
                    services.AddMessageSendingGateway(topicSendingEndpointConfig);
                    services.AddMessageSendingGateway(queueEndpointConfig);
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddBusDepotApplicationInsights();
                })
                .UseConsoleLifetime();

            ILogger<Program> logger = null;
            try
            {
                var host = hostBuilder.Build();
                logger = host.Services.GetService<ILogger<Program>>();

                var runTask = host.RunAsync();
                var eventSenderTask = SendEventsToSingleTypeTopic(host, count: 1);
                var commandSenderTask = SendCommandsOfMultipleTypesToQueue(host, count: 1);

                await Task.WhenAll(runTask, eventSenderTask, commandSenderTask).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(0, ex, $"Unhandled exception in SampleConsoleApp:{Environment.NewLine}{ex}");
            }
            finally
            {
                await Task.Delay(3000);
            }
        }

        private static async Task SendEventsToSingleTypeTopic(IHost host, int count)
        {
            var sendingGateway = host.Services.GetRequiredService<IMessageSendingGateway<SingleMessageTypeEndpointConfig>>();

            var events = Enumerable.Range(0, count)
                .Select(i => new MyEvent {Id = i, Name = $"Hello from {nameof(MyEvent)} {i}"});

            await sendingGateway.SendMultipleAsync(events).ConfigureAwait(false);
        }

        private static async Task SendCommandsOfMultipleTypesToQueue(IHost host, int count)
        {
            var sendingGateway = host.Services.GetRequiredService<IMessageSendingGateway<MultiMessageTypeEndpointHandlingConfig>>();

            var firstCommands = Enumerable.Range(0, count)
                .Select(i => new MyFirstCommand { Id = i, Name = $"Hello from {nameof(MyFirstCommand)} {i}" });
            var secondCommands = Enumerable.Range(0, count)
                .Select(i => new MySecondCommand { Id = i, Name = $"Hello from {nameof(MySecondCommand)} {i}" });

            var firstCommandTask = sendingGateway.SendMultipleAsync(firstCommands);
            var secondCommandTask = sendingGateway.SendMultipleAsync(secondCommands);

            await Task.WhenAll(firstCommandTask, secondCommandTask).ConfigureAwait(false);
        }
    }
}
