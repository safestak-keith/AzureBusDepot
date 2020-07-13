using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.Samples.NetCoreConsoleApp.SingleMessageType
{
    public class MyEventHandler : IMessageHandler<MyEvent>
    {
        private const int ApiCallElapsed = 1;
        private const int DbPersisting = 2;
        private const int DbPersistElapsed = 3;

        private readonly ILogger _logger;
        private readonly IInstrumentor _instrumentor;

        public MyEventHandler(ILogger<MyEventHandler> logger, IInstrumentor instrumentor)
        {
            _logger = logger;
            _instrumentor = instrumentor;
        }

        public async Task<MessageHandlingResult> HandleMessageAsync(
            MyEvent message, MessageContext context, CancellationToken ct)
        {
            _logger.LogDebug(LogEventIds.HandlerStarted, $"{nameof(MyEventHandler)}:{nameof(HandleMessageAsync)} started");
            
            try
            {
                // Just some fake tasks to mimic doing something
                var associatedItem = await FakeCallToHttpApiToGetAssociatedItem(message, ct).ConfigureAwait(false);

                var dbContractItem = (message.Id, message.Name, associatedItem.Id, associatedItem.Name);

                await FakeCallToPersistToSomeDatabase(dbContractItem, ct);
            }
            catch (DivideByZeroException ex)
            {
                _logger.LogError(LogEventIds.HandlerException, ex, $"Unhandled exception in {nameof(MyEventHandler)}");
                return MessageHandlingResult.DeadLettered(ex, context.UserProperties);
            }

            _logger.LogDebug(LogEventIds.HandlerFinished, $"{nameof(MyEventHandler)}:{nameof(HandleMessageAsync)} finished");

            return MessageHandlingResult.Completed(context.UserProperties);
        }

        private async Task<(Guid Id, string Name)> FakeCallToHttpApiToGetAssociatedItem(MyEvent message, CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            await Task.Delay(80, ct).ConfigureAwait(false);

            _instrumentor.TrackDependency(ApiCallElapsed, stopwatch.ElapsedMilliseconds, DateTimeOffset.Now, "HTTP", "SomeHTTPApi", "GET", $"Id: {message.Id}");

            return (Guid.NewGuid(), message.Name);
        }

        private async Task FakeCallToPersistToSomeDatabase((int Id, string Name, Guid AssociatedGuid, string AssociatedItemName) dbContract, CancellationToken ct)
        {
            _logger.LogDebug(DbPersisting, "Saving item to some database", dbContract);

            var stopwatch = Stopwatch.StartNew();

            await Task.Delay(50, ct).ConfigureAwait(false);

            _instrumentor.TrackDependency(DbPersistElapsed, stopwatch.ElapsedMilliseconds, DateTimeOffset.Now, "Database", "SomeSQL", "Insert", $"Id: {dbContract.Id}");
        }
    }
}
