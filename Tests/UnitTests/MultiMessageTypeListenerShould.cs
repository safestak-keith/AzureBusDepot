using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureBusDepot.UnitTests
{
    public class MultiMessageTypeListenerShould
    {
        private readonly MultiMessageTypeListener<MyEndpointHandlingConfig> _listener;
        private readonly SpyLogger<MultiMessageTypeListener<MyEndpointHandlingConfig>> _spyLogger;
        private readonly Mock<IMessageProcessorDispatcher> _mockProcessorDispatcher;
        private readonly Mock<IMessageProcessor<MyEvent>> _mockProcessor;
        private readonly Mock<IMessageReceiverFactory> _mockMessageReceiverFactory;
        private readonly Mock<IInstrumentor> _mockInstrumentor;
        private readonly SpyMessageReceiver _spyMessageReceiver;
        private readonly MyEndpointHandlingConfig _options = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.multimessagetype/Subscriptions/MultiMessageTypeListenerShould",
            MaxConcurrentCalls = 2,
            MaxAutoRenewDurationSeconds = 60,
            AutoComplete = false,
        };

        public MultiMessageTypeListenerShould()
        {
            _spyLogger = new SpyLogger<MultiMessageTypeListener<MyEndpointHandlingConfig>>();
            _spyMessageReceiver = new SpyMessageReceiver(_options);
            _mockMessageReceiverFactory = new Mock<IMessageReceiverFactory>();
            _mockMessageReceiverFactory.Setup(m => m.CreateMessageReceiver(_options, null)).Returns(_spyMessageReceiver);
            _mockInstrumentor = new Mock<IInstrumentor>();
            _mockProcessor = new Mock<IMessageProcessor<MyEvent>>();
            _mockProcessor
                .Setup(m => m.ProcessMessageAsync(It.IsAny<Message>(), _options, CancellationToken.None))
                .ReturnsAsync(MessageHandlingResult.Completed());
            _mockProcessorDispatcher = new Mock<IMessageProcessorDispatcher>();
            _mockProcessorDispatcher.Setup(m => m.GetProcessorForMessage(It.IsAny<Message>())).Returns(_mockProcessor.Object);
                
            _listener = new MultiMessageTypeListener<MyEndpointHandlingConfig>(
                _spyLogger,
                _mockProcessorDispatcher.Object,
                _mockMessageReceiverFactory.Object,
                _mockInstrumentor.Object,
                _options);
        }

        [Fact]
        public async Task Register_Handler_Given_Correctly_Configured_TopicHandlingOptions_When_Listening_Is_Started()
        {
            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            _spyMessageReceiver.RegisterMessageHandlerCalled.Should().BeTrue();
            _spyMessageReceiver.Handler.Should().NotBeNull();
            _spyMessageReceiver.MessageHandlerOptions.ExceptionReceivedHandler.Should().NotBeNull();
            _spyMessageReceiver.MessageHandlerOptions.AutoComplete.Should().Be(_options.AutoComplete);
            _spyMessageReceiver.ServiceBusConnection.Endpoint.AbsoluteUri.Should().Be("sb://your-sb.windows.net/");
            _spyMessageReceiver.Path.Should().Be(_options.EntityPath);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task Dispatch_A_Processor_For_Every_Message_Given_Correctly_Configured_TopicHandlingOptions_When_Handling_Messages(int count)
        {
            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await ReceiveMessages(count).ConfigureAwait(false);

            _mockProcessorDispatcher.Verify(
                p => p.GetProcessorForMessage(It.IsAny<Message>()),
                Times.Exactly(count));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task Process_Messages_Given_Correctly_Configured_TopicHandlingOptions_When_Handling_Messages(int count)
        {
            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await ReceiveMessages(count).ConfigureAwait(false);

            _mockProcessor.Verify(
                p => p.ProcessMessageAsync(It.IsAny<Message>(), _options, It.IsAny<CancellationToken>()), 
                Times.Exactly(count));
        }

        [Fact]
        public async Task Complete_A_Message_Given_Correctly_Configured_TopicHandlingOptions_And_Result_Is_Completed_When_Handling_Messages()
        {
            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await ReceiveMessages(1).ConfigureAwait(false);

            _spyMessageReceiver.CompleteAsyncCalled.Should().BeTrue();
        }

        [Fact]
        public async Task DeadLetters_A_Message_Given_Correctly_Configured_TopicHandlingOptions_And_Result_Is_Errored_When_Handling_Messages()
        {
            _mockProcessor.Reset();
            _mockProcessor
                .Setup(m => m.ProcessMessageAsync(It.IsAny<Message>(), _options, CancellationToken.None))
                .ReturnsAsync(MessageHandlingResult.DeadLettered(new DivideByZeroException()));

            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await ReceiveMessages(1).ConfigureAwait(false);

            _spyMessageReceiver.DeadLetterAsyncCalled.Should().BeTrue();
        }

        [Fact]
        public async Task DeadLetters_A_Message_Given_Correctly_Configured_TopicHandlingOptions_And_Result_Is_UnrecognisedMessageType_When_Handling_Messages()
        {
            _mockProcessor.Reset();
            _mockProcessor
                .Setup(m => m.ProcessMessageAsync(It.IsAny<Message>(), _options, CancellationToken.None))
                .ReturnsAsync(MessageHandlingResult.UnrecognisedMessageType("Bad ting"));

            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await ReceiveMessages(1).ConfigureAwait(false);

            _spyMessageReceiver.DeadLetterAsyncCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Abandons_A_Message_Given_Correctly_Configured_TopicHandlingOptions_And_Result_Is_Abandoned_When_Handling_Messages()
        {
            _mockProcessor.Reset();
            _mockProcessor
                .Setup(m => m.ProcessMessageAsync(It.IsAny<Message>(), _options, CancellationToken.None))
                .ReturnsAsync(MessageHandlingResult.Abandoned(new AbandonedMutexException()));

            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await ReceiveMessages(1).ConfigureAwait(false);

            _spyMessageReceiver.AbandonAsyncCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Closes_Receiver_On_Close_Given_Correctly_Configured_TopicHandlingOptions_When_Listening_Is_Stopped()
        {
            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);
            await _listener.StopListeningAsync().ConfigureAwait(false);

            _spyMessageReceiver.CloseAsyncCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Log_Exception_Given_Receiving_Exception_Is_Thrown_When_Handling_Messages()
        {
            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await _spyMessageReceiver
                .ExceptionReceivedHandler(new ExceptionReceivedEventArgs(new DivideByZeroException(), "8/0", "", "", ""))
                .ConfigureAwait(false);

            _spyLogger.HasExceptionBeenLogged(LogLevel.Error, LogEventIds.ListenerException, new DivideByZeroException())
                .Should().BeTrue();
        }

        [Fact]
        public async Task Log_Warning_Given_Receiving_OperationCancelledException_Is_Thrown_When_Handling_Messages()
        {
            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await _spyMessageReceiver
                .ExceptionReceivedHandler(new ExceptionReceivedEventArgs(new OperationCanceledException(), "8/0", "", "", ""))
                .ConfigureAwait(false);

            _spyLogger.HasExceptionBeenLogged(LogLevel.Warning, LogEventIds.ListenerCancelled, new OperationCanceledException())
                .Should().BeTrue();
        }

        [Theory]
        [InlineData(1, true, "hello")]
        [InlineData(5, false, "g'day")]
        public async Task Track_Request_Telemetry_When_Handling_Messages(int count, bool isSuccessful, string customProperty)
        {
            var customProperties = new Dictionary<string, object> { { "customProperty", customProperty } };
            _mockProcessor.Reset();
            _mockProcessor
                .Setup(m => m.ProcessMessageAsync(It.IsAny<Message>(), _options, CancellationToken.None))
                .ReturnsAsync(isSuccessful ? MessageHandlingResult.Completed(customProperties) : MessageHandlingResult.DeadLettered("BOOM", customProperties));

            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await ReceiveMessages(count).ConfigureAwait(false);

            _mockInstrumentor.Verify(
                i => i.TrackRequest(
                    LogEventIds.ListenerHandlerFinished, It.IsAny<long>(), It.IsAny<DateTimeOffset>(), "MultiMessageTypeListener<MyEndpointHandlingConfig>", null, null, isSuccessful, It.Is<IDictionary<string, object>>(d => (string)d["customProperty"] == customProperty)),
                Times.Exactly(count));
        }

        private async Task ReceiveMessages(int count)
        {
            // Faking message handling within a receive
            var handlerTasks = Enumerable.Range(0, count)
                .Select(i => _spyMessageReceiver.Handler(NewMessageWithBody($"{{\"Id\":{i}, \"Name\": \"{i}\"}}"), CancellationToken.None));

            await Task.WhenAll(handlerTasks).ConfigureAwait(false);
        }

        private static Message NewMessageWithBody(string body)
        {
            var message = new Message(Encoding.UTF8.GetBytes(body))
            {
                TimeToLive = TimeSpan.FromHours(6),
                ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddMinutes(-15),
            };

            // Reflection required to set internal property
            var messageSystemPropertiesType = message.SystemProperties.GetType();
            var prop = messageSystemPropertiesType.GetProperty("SequenceNumber");
            prop.SetValue(message.SystemProperties, 1, null);

            return message;
        }
    }
}
