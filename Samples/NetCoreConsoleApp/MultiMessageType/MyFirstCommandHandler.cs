using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.Samples.NetCoreConsoleApp.MultiMessageType
{
    public class MyFirstCommandHandler : IMessageHandler<MyFirstCommand>
    {
        private readonly ILogger _logger;
        private readonly IInstrumentor _instrumentor;

        public MyFirstCommandHandler(ILogger<MyFirstCommandHandler> logger, IInstrumentor instrumentor)
        {
            _logger = logger;
            _instrumentor = instrumentor;
        }

        public async Task<MessageHandlingResult> HandleMessageAsync(
            MyFirstCommand message, MessageContext context, CancellationToken ct)
        {
            _logger.LogDebug(LogEventIds.HandlerStarted, $"{nameof(MyFirstCommandHandler)}:{nameof(HandleMessageAsync)} started");
            
            try
            {
                await FakeCallToHttpApiToPutAssociatedItem(message, ct).ConfigureAwait(false);

                await FakeSendingOfMessage(message, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEventIds.HandlerException, ex, $"Unhandled exception in {nameof(MyFirstCommandHandler)}");
                return MessageHandlingResult.DeadLettered(ex, context.UserProperties);
            }

            _logger.LogDebug(LogEventIds.HandlerFinished, $"{nameof(MyFirstCommandHandler)}:{nameof(HandleMessageAsync)} finished");

            return MessageHandlingResult.Completed(context.UserProperties);
        }

        private async Task<(Guid Id, string Name)> FakeCallToHttpApiToPutAssociatedItem(MyFirstCommand message, CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            await Task.Delay(1, ct).ConfigureAwait(false);
            _instrumentor.TrackDependency(2, stopwatch.ElapsedMilliseconds, DateTimeOffset.Now, "HTTP", "SomeAPI", "PUT", $"Id: {message.Id}");

            return (Guid.NewGuid(), message.Name);
        }

        private async Task FakeSendingOfMessage(MyFirstCommand message, CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            await Task.Delay(1, ct).ConfigureAwait(false);
            _instrumentor.TrackDependency(1, stopwatch.ElapsedMilliseconds, DateTimeOffset.Now, "Messaging", "AzureServiceBus", "Insert", $"Id: {message.Id}");

        }
    }
}
