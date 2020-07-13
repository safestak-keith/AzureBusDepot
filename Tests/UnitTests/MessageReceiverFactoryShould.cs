using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Xunit;

namespace AzureBusDepot.UnitTests
{
    public class MessageReceiverFactoryShould
    {
        private readonly MessageReceiverFactory _messageReceiverFactory;

        public MessageReceiverFactoryShould()
        {
            _messageReceiverFactory = new MessageReceiverFactory();
        }

        [Fact]
        public void Create_New_Message_Receivers_Regardless_Of_Having_The_Same_Config()
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

            var messageReceiver = _messageReceiverFactory.CreateMessageReceiver(sameConfig);
            var anotherMessageReceiver = _messageReceiverFactory.CreateMessageReceiver(sameConfig);

            messageReceiver.Should().NotBeNull();
            anotherMessageReceiver.Should().NotBeNull();
            messageReceiver.Should().NotBeSameAs(anotherMessageReceiver);
        }

        [Theory]
        [InlineData("Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=", "sb://your-sb.windows.net/")]
        [InlineData("Endpoint=sb://my-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=", "sb://my-sb.windows.net/")]
        public void Create_Message_Receiver_With_Right_Endpoint(string connectionString, string expectedEndpoint)
        {
            var messageReceiver = _messageReceiverFactory.CreateMessageReceiver(
                new MyEndpointHandlingConfig
                {
                    ConnectionString = connectionString,
                    EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                    MaxConcurrentCalls = 2,
                    MaxAutoRenewDurationSeconds = 60,
                    AutoComplete = true,
                });

            messageReceiver.ServiceBusConnection.Endpoint.AbsoluteUri.Should().Be(expectedEndpoint);
        }

        [Theory]
        [InlineData("somequeue")]
        [InlineData("sometopic/subscriptions/somesubscription")]
        [InlineData("unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould")]
        public void Create_Message_Receiver_With_Right_Path(string path)
        {
            var messageReceiver = _messageReceiverFactory.CreateMessageReceiver(
                new MyEndpointHandlingConfig
                {
                    ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                    EntityPath = path,
                    MaxConcurrentCalls = 2,
                    MaxAutoRenewDurationSeconds = 60,
                    AutoComplete = true,
                });

            messageReceiver.Path.Should().Be(path);
        }

        [Fact]
        public void Create_Message_Receiver_With_RetryExponential_Policy()
        {
            var messageReceiver = _messageReceiverFactory.CreateMessageReceiver(
                new MyEndpointHandlingConfig
                {
                    ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                    EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                    MaxConcurrentCalls = 2,
                    MaxAutoRenewDurationSeconds = 60,
                    AutoComplete = true,
                });

            var retryPolicy = messageReceiver.ServiceBusConnection.RetryPolicy;
            retryPolicy.Should().BeOfType<RetryExponential>();
            retryPolicy.As<RetryExponential>().MaxRetryCount.Should().Be(MessageReceiverFactory.DefaultRetryPolicy.MaxRetryCount);
            retryPolicy.As<RetryExponential>().MinimalBackoff.Should().Be(MessageReceiverFactory.DefaultRetryPolicy.MinimalBackoff);
            retryPolicy.As<RetryExponential>().MaximumBackoff.Should().Be(MessageReceiverFactory.DefaultRetryPolicy.MaximumBackoff);
        }

        [Fact]
        public void Create_Message_Receiver_With_NoRetry_Policy_Given_Policy_Is_Passed_In()
        {
            var messageReceiver = _messageReceiverFactory.CreateMessageReceiver(
                new MyEndpointHandlingConfig
                {
                    ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                    EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                    MaxConcurrentCalls = 2,
                    MaxAutoRenewDurationSeconds = 60,
                    AutoComplete = true,
                }, new NoRetry());

            (messageReceiver as MessageReceiver).RetryPolicy
                .Should().BeOfType<NoRetry>();
        }


        #region Dead letter message receiver tests

        [Fact]
        public void Create_Dead_Letter_Message_Receiver_With_Expected_Properties()
        {
            var config = new MyEndpointHandlingConfig
            {
                ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                MaxConcurrentCalls = 2,
                MaxAutoRenewDurationSeconds = 60,
                AutoComplete = true,
            };

            var expectedServiceBusEndpoint = "sb://your-sb.windows.net/";
            var expectedDeadLetterPath = EntityNameHelper.FormatDeadLetterPath(config.EntityPath);

            var messageReceiver = _messageReceiverFactory.CreateDeadLetterMessageReceiver(config);

            (messageReceiver as MessageReceiver).ServiceBusConnection.Endpoint.Should().Be(expectedServiceBusEndpoint);
            (messageReceiver as MessageReceiver).Path.Should().Be(expectedDeadLetterPath);

            // Defaults
            (messageReceiver as MessageReceiver).ReceiveMode.Should().Be(ReceiveMode.PeekLock);
            (messageReceiver as MessageReceiver).RetryPolicy.Should().Be(MessageReceiverFactory.DefaultRetryPolicy);
            (messageReceiver as MessageReceiver).PrefetchCount.Should().Be(0);
        }


        [Fact]
        public void Create_Dead_Letter_Message_Receiver_With_Expected_NonDefault_Properties()
        {
            var config = new MyEndpointHandlingConfig
            {
                ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                EntityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould",
                MaxConcurrentCalls = 2,
                MaxAutoRenewDurationSeconds = 60,
                AutoComplete = true,
            };

            var expectedServiceBusEndpoint = "sb://your-sb.windows.net/";
            var expectedDeadLetterPath = EntityNameHelper.FormatDeadLetterPath(config.EntityPath);
            var expectedReceiveMode = ReceiveMode.ReceiveAndDelete;
            var expectedRetryPolicy = new NoRetry();
            var expectedPrefetchCount = 1;

            var messageReceiver = _messageReceiverFactory.CreateDeadLetterMessageReceiver(config, expectedReceiveMode, expectedRetryPolicy, expectedPrefetchCount);

            (messageReceiver as MessageReceiver).ServiceBusConnection.Endpoint.Should().Be(expectedServiceBusEndpoint);
            (messageReceiver as MessageReceiver).Path.Should().Be(expectedDeadLetterPath);
            (messageReceiver as MessageReceiver).ReceiveMode.Should().Be(expectedReceiveMode);
            (messageReceiver as MessageReceiver).RetryPolicy.Should().Be(expectedRetryPolicy);
            (messageReceiver as MessageReceiver).PrefetchCount.Should().Be(expectedPrefetchCount);
        }

        #endregion

        [Fact]
        public void Create_Message_Receiver_With_Expected_Null_RetryPolicy_Fallsback_To_Default()
        {
            var connectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=";
            var entityPath = "unittests.singlemessagetype/Subscriptions/MessageReceiverFactoryShould";

            var messageReceiver = _messageReceiverFactory.CreateMessageReceiver(connectionString, entityPath, retryPolicy: null);

            (messageReceiver as MessageReceiver).RetryPolicy
                .Should().Be(MessageReceiverFactory.DefaultRetryPolicy);
        }
    }
}