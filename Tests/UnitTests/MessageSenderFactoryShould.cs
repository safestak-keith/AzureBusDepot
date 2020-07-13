using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Xunit;

namespace AzureBusDepot.UnitTests
{
    public class MessageSenderFactoryShould
    {
        private readonly MessageSenderFactory _messageSenderFactory;

        public MessageSenderFactoryShould()
        {
            _messageSenderFactory = new MessageSenderFactory();
        }

        [Fact]
        public void Create_New_Message_Senders_Regardless_Of_Having_The_Same_Config()
        {
            var sameConfig = new MyEndpointHandlingConfig
            {
                ConnectionString =
                    "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                MaxConcurrentCalls = 2,
                MaxAutoRenewDurationSeconds = 60,
                AutoComplete = true,
            };

            var messageSender = _messageSenderFactory.CreateMessageSender(sameConfig);
            var anotherMessageSender = _messageSenderFactory.CreateMessageSender(sameConfig);

            messageSender.Should().NotBeNull();
            anotherMessageSender.Should().NotBeNull();
            messageSender.Should().NotBeSameAs(anotherMessageSender);
        }

        [Theory]
        [InlineData("Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=", "sb://your-sb.windows.net/")]
        [InlineData("Endpoint=sb://my-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=", "sb://my-sb.windows.net/")]
        public void Create_Message_Sender_With_Right_Endpoint(string connectionString, string expectedEndpoint)
        {
            var messageSender = _messageSenderFactory.CreateMessageSender(
                new MyEndpointHandlingConfig
                {
                    ConnectionString = connectionString,
                    EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                    MaxConcurrentCalls = 2,
                    MaxAutoRenewDurationSeconds = 60,
                    AutoComplete = true,
                });

            messageSender.ServiceBusConnection.Endpoint.AbsoluteUri.Should().Be(expectedEndpoint);
        }

        [Theory]
        [InlineData("somequeue")]
        [InlineData("sometopic/subscriptions/somesubscription")]
        [InlineData("unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould")]
        public void Create_Message_Sender_With_Right_Path(string path)
        {
            var messageSender = _messageSenderFactory.CreateMessageSender(
                new MyEndpointHandlingConfig
                {
                    ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                    EntityPath = path,
                    MaxConcurrentCalls = 2,
                    MaxAutoRenewDurationSeconds = 60,
                    AutoComplete = true,
                });

            messageSender.Path.Should().Be(path);
        }

        [Fact]
        public void Create_Message_Sender_With_Default_RetryExponential_Policy()
        {
            var messageSender = _messageSenderFactory.CreateMessageSender(
                new MyEndpointHandlingConfig
                {
                    ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                    EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                    MaxConcurrentCalls = 2,
                    MaxAutoRenewDurationSeconds = 60,
                    AutoComplete = true,
                });

            var retryPolicy = messageSender.ServiceBusConnection.RetryPolicy;
            retryPolicy.Should().BeOfType<RetryExponential>();
            retryPolicy.As<RetryExponential>().MaxRetryCount.Should().Be(MessageSenderFactory.DefaultRetryPolicy.MaxRetryCount);
            retryPolicy.As<RetryExponential>().MinimalBackoff.Should().Be(MessageSenderFactory.DefaultRetryPolicy.MinimalBackoff);
            retryPolicy.As<RetryExponential>().MaximumBackoff.Should().Be(MessageSenderFactory.DefaultRetryPolicy.MaximumBackoff);
        }

        [Fact]
        public void Create_Message_Sender_With_NoRetry_Policy_Given_Policy_Is_Passed_In()
        {
            var messageSender = _messageSenderFactory.CreateMessageSender(
                new MyEndpointHandlingConfig
                {
                    ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                    EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                    MaxConcurrentCalls = 2,
                    MaxAutoRenewDurationSeconds = 60,
                    AutoComplete = true,
                }, new NoRetry());

            (messageSender as MessageSender).RetryPolicy
                .Should().BeOfType<NoRetry>();
        }
    }
}