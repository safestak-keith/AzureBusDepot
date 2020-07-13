using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Moq;
using Xunit;

namespace AzureBusDepot.UnitTests
{
    public class MessageProcessorShould
    {
        private readonly MessageProcessor<MyEvent> _processor;
        private readonly SpyLogger<MessageProcessor<MyEvent>> _spyLogger;
        private readonly Mock<IMessageHandler<MyEvent>> _mockHandler;
        private readonly Mock<IMessageSerialiser> _mockSerialiser;
        private readonly Mock<IInstrumentor> _mockInstrumentor;

        public MessageProcessorShould()
        {
            _spyLogger = new SpyLogger<MessageProcessor<MyEvent>>();
            _mockHandler = new Mock<IMessageHandler<MyEvent>>();
            _mockSerialiser = new Mock<IMessageSerialiser>();
            _mockInstrumentor = new Mock<IInstrumentor>();
            _processor = new MessageProcessor<MyEvent>(_spyLogger, _mockSerialiser.Object, _mockHandler.Object, _mockInstrumentor.Object);
        }

        [Fact]
        public async Task Attempt_To_Deserialise_Message_To_Expected_Type_Given_Valid_Message()
        {
            var message = NewMessageWithBody();
            _mockSerialiser
                .Setup(s => s.Deserialise<MyEvent>(message))
                .Returns(MyEvent.Default);
            _mockHandler
                .Setup(h => h.HandleMessageAsync(MyEvent.Default, It.IsAny<MessageContext>(), CancellationToken.None))
                .ReturnsAsync(MessageHandlingResult.Completed());

            await _processor.ProcessMessageAsync(message, MyEndpointHandlingConfig.Default, CancellationToken.None).ConfigureAwait(false);

            _mockSerialiser.Verify(s => s.Deserialise<MyEvent>(message), Times.Once);
        }

        [Fact]
        public async Task Handle_Deserialised_Message_Given_Valid_Message()
        {
            var message = NewMessageWithBody();
            _mockSerialiser
                .Setup(s => s.Deserialise<MyEvent>(It.IsAny<Message>()))
                .Returns(MyEvent.Default);
            _mockHandler
                .Setup(h => h.HandleMessageAsync(MyEvent.Default, It.IsAny<MessageContext>(), CancellationToken.None))
                .ReturnsAsync(MessageHandlingResult.Completed());

            await _processor.ProcessMessageAsync(message, MyEndpointHandlingConfig.Default, CancellationToken.None).ConfigureAwait(false);

            _mockHandler.Verify(s => s.HandleMessageAsync(MyEvent.Default, It.IsAny<MessageContext>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Measure_Elapsed_Duration_Given_Successful_Handling()
        {
            var message = NewMessageWithBody();
            _mockSerialiser.Setup(s => s.Deserialise<MyEvent>(It.IsAny<Message>())).Returns(MyEvent.Default);

            await _processor.ProcessMessageAsync(message, MyEndpointHandlingConfig.Default, CancellationToken.None).ConfigureAwait(false);

            _mockInstrumentor.Verify(s => s.TrackElapsed(LogEventIds.HandlerMeasuredElapsed, It.IsAny<long>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task Track_User_Properties_Given_Successful_Handling()
        {
            var message = NewMessageWithBody();
            _mockSerialiser.Setup(s => s.Deserialise<MyEvent>(It.IsAny<Message>())).Returns(MyEvent.Default);

            await _processor.ProcessMessageAsync(message, MyEndpointHandlingConfig.Default, CancellationToken.None).ConfigureAwait(false);

            _mockInstrumentor.Verify(s => s.TrackElapsed(LogEventIds.HandlerMeasuredElapsed, It.IsAny<long>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task Throws_Exception_Given_Unhandled_Exception_Is_Thrown()
        {
            var message = NewMessageWithBody();
            _mockSerialiser
                .Setup(s => s.Deserialise<MyEvent>(It.IsAny<Message>()))
                .Returns(MyEvent.Default);
            _mockHandler
                .Setup(h => h.HandleMessageAsync(MyEvent.Default, It.IsAny<MessageContext>(), CancellationToken.None))
                .Throws<DivideByZeroException>();

            await Assert.ThrowsAsync<DivideByZeroException>(async () => await _processor.ProcessMessageAsync(message, MyEndpointHandlingConfig.Default, CancellationToken.None).ConfigureAwait(false));
        }

        [Fact]
        public async Task Return_An_Abandoned_Result_Given_OperationCanceledException_Is_Thrown()
        {
            var message = NewMessageWithBody();
            _mockSerialiser
                .Setup(s => s.Deserialise<MyEvent>(It.IsAny<Message>()))
                .Returns(MyEvent.Default);
            _mockHandler
                .Setup(h => h.HandleMessageAsync(MyEvent.Default, It.IsAny<MessageContext>(), CancellationToken.None))
                .Throws<OperationCanceledException>();

            var result = await _processor.ProcessMessageAsync(message, MyEndpointHandlingConfig.Default, CancellationToken.None).ConfigureAwait(false);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.Abandoned);
        }

        [Theory]
        [InlineData("key", "value")]
        [InlineData("Name", "labcabincalifornia")]
        public async Task Capture_User_Propertries_Given_OperationCanceledException_Is_Thrown(string key, string value)
        {
            var message = NewMessageWithBody(userPropKey:key, userPropValue:value);
            _mockSerialiser
                .Setup(s => s.Deserialise<MyEvent>(It.IsAny<Message>()))
                .Returns(MyEvent.Default);
            _mockHandler
                .Setup(h => h.HandleMessageAsync(MyEvent.Default, It.IsAny<MessageContext>(), CancellationToken.None))
                .Throws<OperationCanceledException>();

            var result = await _processor.ProcessMessageAsync(message, MyEndpointHandlingConfig.Default, CancellationToken.None).ConfigureAwait(false);

            result.AdditionalProperties[key].Should().Be(value);
        }

        private static Message NewMessageWithBody(
            string body = "{\"Id\":1, \"Name\": \"1\"}", string userPropKey = null, string userPropValue = null)
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

            if (userPropKey != null)
            {
                message.UserProperties.Add(userPropKey, userPropValue);
            }

            return message;
        }
    }
}
