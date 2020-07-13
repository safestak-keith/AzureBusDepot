using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace AzureBusDepot.AdapterTests
{
    public class MultiMessageTypeListenerShould
    {
        private readonly MultiMessageTypeListener<MyEndpointHandlingConfig> _listener;
        private readonly Mock<IMessageProcessorDispatcher> _mockProcessorDispatcher;
        private readonly Mock<IMessageProcessor<MyEvent>> _mockProcessor;
        private readonly MyEndpointHandlingConfig _sendingOptions = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.multimessagetype"
        };
        private readonly MyEndpointHandlingConfig _options = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.multimessagetype/Subscriptions/MultiMessageTypeListenerShould",
            MaxConcurrentCalls = 2,
            MaxAutoRenewDurationSeconds = 60,
            AutoComplete = false
        };

        public MultiMessageTypeListenerShould()
        {
            _mockProcessor = new Mock<IMessageProcessor<MyEvent>>();
            _mockProcessor
                .Setup(m => m.ProcessMessageAsync(It.IsAny<Message>(), _options, CancellationToken.None))
                .ReturnsAsync(MessageHandlingResult.Completed());
            _mockProcessorDispatcher = new Mock<IMessageProcessorDispatcher>();
            _mockProcessorDispatcher.Setup(m => m.GetProcessorForMessage(It.IsAny<Message>())).Returns(_mockProcessor.Object);
                
            _listener = new MultiMessageTypeListener<MyEndpointHandlingConfig>(
                new NullLogger<MultiMessageTypeListener<MyEndpointHandlingConfig>>(),
                _mockProcessorDispatcher.Object,
                new MessageReceiverFactory(), 
                new VoidInstrumentor(), 
                _options);
        }

        [Theory(Skip = "Only to be run locally (with correct connection string)")]
        [InlineData(5)]
        public async Task Process_Messages_Given_Correctly_Configured_TopicHandlingOptions(int count)
        {
            await _listener.StartListeningAsync(CancellationToken.None).ConfigureAwait(false);

            await SendEvents(count, delayMillis:5).ConfigureAwait(false);

            await Task.Delay(1000).ConfigureAwait(false);

            _mockProcessor.Verify(
                p => p.ProcessMessageAsync(It.IsAny<Message>(), _options, It.IsAny<CancellationToken>()), 
                Times.AtLeast(count));
        }

        private async Task SendEvents(int count, int delayMillis)
        {
            var client = new TopicClient(_sendingOptions.ConnectionString, _sendingOptions.EntityPath);
            for (var i = 0; i < count; i++)
            {
                await Task.Delay(delayMillis).ConfigureAwait(false);
                var myEvent = new MyEvent { Id = i, Name = $"Hello Keith {i}" };
                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(myEvent)))
                {
                    Label = i.ToString(),
                    CorrelationId = Guid.NewGuid().ToString()
                };
                await client.SendAsync(message).ConfigureAwait(false);
            }
        }
    }
}
