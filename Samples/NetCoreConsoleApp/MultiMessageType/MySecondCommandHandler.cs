using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.Samples.NetCoreConsoleApp.MultiMessageType
{
    public class MySecondCommandHandler : IMessageHandler<MySecondCommand>
    {
        private const int DbPersistElapsed = 3;

        private readonly ILogger _logger;
        private readonly IInstrumentor _instrumentor;

        public MySecondCommandHandler(ILogger<MySecondCommandHandler> logger, IInstrumentor instrumentor)
        {
            _logger = logger;
            _instrumentor = instrumentor;
        }

        public async Task<MessageHandlingResult> HandleMessageAsync(
            MySecondCommand message, MessageContext context, CancellationToken ct)
        {
            _logger.LogDebug(LogEventIds.HandlerStarted, $"{nameof(MySecondCommandHandler)}:{nameof(HandleMessageAsync)} started");
            
            try
            {
                await FakeCallToPersistToSomeDatabase(message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEventIds.HandlerException, ex, $"Unhandled exception in {nameof(MySecondCommandHandler)}");
                return MessageHandlingResult.DeadLettered(ex, context.UserProperties);
            }

            _logger.LogDebug(LogEventIds.HandlerFinished, $"{nameof(MySecondCommandHandler)}:{nameof(HandleMessageAsync)} finished");

            return MessageHandlingResult.Completed(context.UserProperties);
        }

        private async Task FakeCallToPersistToSomeDatabase(MySecondCommand dbContract, CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();
            await Task.Delay(50, ct).ConfigureAwait(false);
            _instrumentor.TrackDependency(DbPersistElapsed, stopwatch.ElapsedMilliseconds, DateTimeOffset.Now, "Database", "SomeSQL", "Insert", $"Id: {dbContract.Id}");
        }
    }
}
