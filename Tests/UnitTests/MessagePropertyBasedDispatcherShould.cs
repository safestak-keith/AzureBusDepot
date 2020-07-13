using System;
using System.Text;
using AzureBusDepot.Abstractions;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AzureBusDepot.UnitTests
{
    public class MessagePropertyBasedDispatcherShould
    {
        private const string MessageTypePropertyName = "EnclosedMessageType";
        private readonly MessagePropertyBasedDispatcher _dispatcher;
        private readonly Mock<ILogger<MessagePropertyBasedDispatcher>> _mockLogger;
        private readonly IServiceProvider _serviceProvider;

        public MessagePropertyBasedDispatcherShould()
        {
            _mockLogger = new Mock<ILogger<MessagePropertyBasedDispatcher>>();
            _serviceProvider = ConfigureServiceProvider();
            _dispatcher = new MessagePropertyBasedDispatcher(_mockLogger.Object, _serviceProvider, MessageTypePropertyName);
        }

        [Fact]
        public void Return_The_Correct_Processor_Given_Valid_Message_Property_And_Configured_ServiceProvider()
        {
            var processor = _dispatcher.GetProcessorForMessage(NewMessageWithTypeProperty(typeof(MyEvent).AssemblyQualifiedName));

            processor.Should().NotBeNull();
            processor.Should().BeOfType<MessageProcessor<MyEvent>>();
        }

        [Fact]
        public void Return_Null_Processor_Given_Message_Has_No_Type_Property()
        {
            var processor = _dispatcher.GetProcessorForMessage(NewMessageWithTypeProperty(null));

            processor.Should().BeNull();
        }

        [Fact]
        public void Return_Null_Processor_Given_Message_Has_Empty_Type_Property()
        {
            var processor = _dispatcher.GetProcessorForMessage(NewMessageWithTypeProperty(""));

            processor.Should().BeNull();
        }

        [Fact]
        public void Return_Null_Processor_Given_Message_Has_Incorrectly_Qualified_Type_Property()
        {
            var processor = _dispatcher.GetProcessorForMessage(NewMessageWithTypeProperty("Some.Bogus.Namespace.MyEvent"));

            processor.Should().BeNull();
        }

        [Fact]
        public void Return_Null_Processor_Given_ServiceProvider_Has_Not_Added_Processor_Implementation()
        {
            var dispatcher = new MessagePropertyBasedDispatcher(
                new NullLogger<MessagePropertyBasedDispatcher>(), 
                new ServiceCollection().BuildServiceProvider(), // explicitly setting no DI bindings
                MessageTypePropertyName);

            var processor = dispatcher.GetProcessorForMessage(NewMessageWithTypeProperty(typeof(MyEvent).AssemblyQualifiedName));

            processor.Should().BeNull();
        }

        private static IServiceProvider ConfigureServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            return serviceCollection
                .AddLogging()
                .AddTransient<IMessageSerialiser, JsonMessageSerialiser>()
                .AddTransient<IInstrumentor, VoidInstrumentor>()
                .AddTransient<IMessageHandler<MyEvent>, MyEventHandler>()
                .AddTransient<IMessageProcessor<MyEvent>, MessageProcessor<MyEvent>>()
                .BuildServiceProvider();
        }

        private static Message NewMessageWithTypeProperty(string enclosedTypeName = null)
        {
            var message = new Message(Encoding.UTF8.GetBytes("{\"Id\":1, \"Name\": \"1\"}"))
            {
                TimeToLive = TimeSpan.FromHours(6),
                ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddMinutes(-15),
            };

            if (enclosedTypeName != null)
            {
                message.UserProperties.Add(MessageTypePropertyName, enclosedTypeName);
            }

            // Reflection required to set internal property
            var messageSystemPropertiesType = message.SystemProperties.GetType();
            var prop = messageSystemPropertiesType.GetProperty("SequenceNumber");
            prop.SetValue(message.SystemProperties, 1, null);

            return message;
        }
    }
}
