using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace AzureBusDepot.UnitTests
{
    public class MessageSendingGatewayShould
    {
        private static readonly byte[] SerialisedBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(MyEvent.Default));
        private readonly MessageSendingGateway<MyEndpointConfig> _outboundGateway;
        private readonly SpyLogger<MessageSendingGateway<MyEndpointConfig>> _spyLogger;
        private readonly Mock<IMessageSerialiser> _mockSerialiser;
        private readonly Mock<IInstrumentor> _mockInstrumentor;
        private readonly Mock<IMessageSenderFactory> _mockMessageSenderFactory;
        private readonly Mock<IMessageSender> _mockSender;
        private readonly MyEndpointConfig _options = new MyEndpointConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.singlemessagetype/Subscriptions/SingleMessageTypeListenerShould",
        };

        public MessageSendingGatewayShould()
        {
            _spyLogger = new SpyLogger<MessageSendingGateway<MyEndpointConfig>>();
            _mockSerialiser = new Mock<IMessageSerialiser>();
            _mockInstrumentor = new Mock<IInstrumentor>();
            _mockMessageSenderFactory = new Mock<IMessageSenderFactory>();
            _mockSender = new Mock<IMessageSender>();
            _mockSerialiser.Setup(s => s.Serialise(MyEvent.Default)).Returns(SerialisedBytes);
            _mockSender.Setup(m => m.Path).Returns(_options.EntityPath);
            _mockMessageSenderFactory.Setup(m => m.CreateMessageSender(_options, null)).Returns(_mockSender.Object);
            _outboundGateway = new MessageSendingGateway<MyEndpointConfig>(
                _spyLogger,
                _mockSerialiser.Object,
                _mockInstrumentor.Object,
                _mockMessageSenderFactory.Object,
                _options);
        }

        [Fact]
        public async Task Attempt_To_Serialise_Message_From_Expected_Type_Given_Valid_Entity_When_Sending_MessageEntity()
        {
            await _outboundGateway.SendAsync(MyEvent.Default).ConfigureAwait(false);

            _mockSerialiser.Verify(s => s.Serialise(MyEvent.Default), Times.Once);
        }

        [Fact]
        public async Task Attempt_To_Serialise_Message_From_Expected_Type_Given_Valid_Entity_When_Sending_OutboundMessage()
        {
            await _outboundGateway.SendAsync(OutboundMessage<MyEvent>.FromEntity(MyEvent.Default)).ConfigureAwait(false);

            _mockSerialiser.Verify(s => s.Serialise(MyEvent.Default), Times.Once);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(23)]
        public async Task Attempt_To_Serialise_Message_From_Expected_Type_Given_Valid_Entity_When_Sending_Multiple_OutboundMessages(int count)
        {
            var outboundMessages = Enumerable.Range(0, count)
                .Select(i => new OutboundMessage<MyEvent>(new MyEvent {Id = i, Name = i.ToString()}))
                .ToArray();

            await _outboundGateway.SendMultipleAsync(outboundMessages).ConfigureAwait(false);

            _mockSerialiser.Verify(s => s.Serialise(It.IsAny<MyEvent>()), Times.Exactly(count));
        }

        [Theory]
        [InlineData("CustomProperty", "CustomValue")]
        [InlineData("G'day", "World")]
        public async Task Include_Custom_Properties_Given_Valid_Entity_When_Sending_MessageEntity(string key, string value)
        {
            var messageProps = new Dictionary<string, object>
            {
                {key, value},
                {"Extra", "Prop"}
            };

            await _outboundGateway.SendAsync(MyEvent.Default, messageProps).ConfigureAwait(false);

            _mockSender.Verify(s => s.SendAsync(It.Is<Message>(
                m => m.UserProperties.ContainsKey(key) && (string)m.UserProperties[key] == value
                    && m.UserProperties.ContainsKey("Extra") && (string)m.UserProperties["Extra"] == "Prop")),
                Times.Once);
        }

        [Theory]
        [InlineData("CustomProperty", "CustomValue")]
        [InlineData("G'day", "World")]
        public async Task Include_Custom_Properties_Given_Valid_Entity_When_Sending_OutboundMessage(string key, string value)
        {
            var messageProps = new Dictionary<string, object>
            {
                {key, value},
                {"Extra", "Prop"}
            };

            await _outboundGateway.SendAsync(OutboundMessage<MyEvent>.FromEntity(MyEvent.Default, messageProps)).ConfigureAwait(false);

            _mockSender.Verify(s => s.SendAsync(It.Is<Message>(
                    m => m.UserProperties.ContainsKey(key) && (string)m.UserProperties[key] == value
                         && m.UserProperties.ContainsKey("Extra") && (string)m.UserProperties["Extra"] == "Prop")),
                Times.Once);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(23)]
        public async Task Include_Custom_Properties_Given_Valid_Entity_When_Sending_Multiple_OutboundMessages(int count)
        {
            var outboundMessages = Enumerable.Range(0, count)
                .Select(i => new OutboundMessage<MyEvent>(
                    new MyEvent { Id = i, Name = i.ToString() },
                    new Dictionary<string, object> {{ i.ToString(), i.ToString() },{"Extra", "Prop"}}))
                .ToArray();

            await _outboundGateway.SendMultipleAsync(outboundMessages).ConfigureAwait(false);

            _mockSender.Verify(s => s.SendAsync(It.Is<IList<Message>>(l => l.Count == count)), Times.Once);
            for (var i = 0; i < count; i++)
            {
                _mockSender.Verify(s => s.SendAsync(It.Is<IList<Message>>(
                    l => l.Any(m => m.UserProperties.ContainsKey(i.ToString()) &&
                                    (string)m.UserProperties[i.ToString()] == i.ToString()
                                    && m.UserProperties.ContainsKey("Extra") &&
                                    (string)m.UserProperties["Extra"] == "Prop"))));
            }
        }

        [Fact]
        public async Task Send_Serialised_Message_Given_Valid_Message_When_Sending_MessageEntity()
        {
            await _outboundGateway.SendAsync(MyEvent.Default).ConfigureAwait(false);

            _mockSender.Verify(s => s.SendAsync(It.Is<Message>(m => m.Body == SerialisedBytes)), Times.Once);
        }

        [Fact]
        public async Task Send_Serialised_Message_Given_Valid_Message_When_Sending_OutboundMessage()
        {
            await _outboundGateway.SendAsync(OutboundMessage<MyEvent>.FromEntity(MyEvent.Default)).ConfigureAwait(false);

            _mockSender.Verify(s => s.SendAsync(It.Is<Message>(m => m.Body == SerialisedBytes)), Times.Once);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(23)]
        public async Task Send_Messages_Given_Valid_Messages_When_Sending_Multiple_OutboundMessages(int count)
        {
            var outboundMessages = Enumerable.Range(0, count)
                .Select(i => new OutboundMessage<MyEvent>(new MyEvent { Id = i, Name = i.ToString() }))
                .ToArray();

            await _outboundGateway.SendMultipleAsync(outboundMessages).ConfigureAwait(false);

            _mockSender.Verify(s => s.SendAsync(It.Is<IList<Message>>(m => m.Count == count)), Times.Once);
        }

        [Fact]
        public async Task Measure_Dependency_Duration_Given_Successful_Sending_When_Sending_MessageEntity()
        {
            await _outboundGateway.SendAsync(MyEvent.Default).ConfigureAwait(false);

            _mockInstrumentor.Verify(
                s => s.TrackDependency(
                    LogEventIds.OutboundGatewayMeasuredElapsedSingle, 
                    It.IsAny<long>(), 
                    It.IsAny<DateTimeOffset>(),
                    MessageSendingGateway<MyEndpointConfig>.AzureServiceBusDependencyType,
                    _options.EntityPath,
                    "SendAsync", 
                    null, 
                    true, 
                    It.IsAny<IDictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task Measure_Dependency_Duration_Given_Successful_Sending_When_Sending_OutboundMessage()
        {
            await _outboundGateway.SendAsync(OutboundMessage<MyEvent>.FromEntity(MyEvent.Default)).ConfigureAwait(false);

            _mockInstrumentor.Verify(
                s => s.TrackDependency(
                    LogEventIds.OutboundGatewayMeasuredElapsedSingle,
                    It.IsAny<long>(),
                    It.IsAny<DateTimeOffset>(),
                    MessageSendingGateway<MyEndpointConfig>.AzureServiceBusDependencyType,
                    _options.EntityPath,
                    "SendAsync",
                    null,
                    true,
                    It.IsAny<IDictionary<string, object>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(23)]
        public async Task Measure_Dependency_Duration_Given_Successful_Sending_When_Sending_Multiple_OutboundMessages(int count)
        {
            var outboundMessages = Enumerable.Range(0, count)
                .Select(i => new OutboundMessage<MyEvent>(new MyEvent { Id = i, Name = i.ToString() }))
                .ToArray();

            await _outboundGateway.SendMultipleAsync(outboundMessages).ConfigureAwait(false);

            _mockInstrumentor.Verify(
                s => s.TrackDependency(
                    LogEventIds.OutboundGatewayMeasuredElapsedSingle,
                    It.IsAny<long>(),
                    It.IsAny<DateTimeOffset>(),
                    MessageSendingGateway<MyEndpointConfig>.AzureServiceBusDependencyType,
                    _options.EntityPath,
                    "SendMultipleAsync",
                    null,
                    true,
                    It.IsAny<IDictionary<string, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReThrows_Exception_Given_Unhandled_Exception_Is_Thrown_When_Sending_Message()
        {
            _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>())).ThrowsAsync(new DivideByZeroException());

            Func<Task> sendAction = async () => await _outboundGateway.SendAsync(MyEvent.Default).ConfigureAwait(false);

            await sendAction.Should().ThrowAsync<DivideByZeroException>();
        }

        [Fact]
        public async Task ReThrows_Exception_Given_Unhandled_Exception_Is_Thrown_When_Sending_OutboundMessage()
        {
            _mockSender.Setup(s => s.SendAsync(It.IsAny<Message>())).ThrowsAsync(new DivideByZeroException());

            Func<Task> sendAction = async () => await _outboundGateway.SendAsync(OutboundMessage<MyEvent>.FromEntity(MyEvent.Default)).ConfigureAwait(false);

            await sendAction.Should().ThrowAsync<DivideByZeroException>();
        }

        [Theory]
        [InlineData(7)]
        [InlineData(23)]
        public async Task ReThrows_Exception_Given_Unhandled_Exception_Is_Thrown_When_Sending_Multiple_OutboundMessages(int count)
        {
            var outboundMessages = Enumerable.Range(0, count)
                .Select(i => new OutboundMessage<MyEvent>(new MyEvent { Id = i, Name = i.ToString() }))
                .ToArray();
            _mockSender.Setup(s => s.SendAsync(It.IsAny<IList<Message>>())).ThrowsAsync(new DivideByZeroException());

            Func<Task> sendAction = async () => await _outboundGateway.SendMultipleAsync(outboundMessages).ConfigureAwait(false);

            await sendAction.Should().ThrowAsync<DivideByZeroException>();
        }
    }
}
