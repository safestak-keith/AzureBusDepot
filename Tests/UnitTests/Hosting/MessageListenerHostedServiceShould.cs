using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using AzureBusDepot.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AzureBusDepot.UnitTests.Hosting
{
    public class MessageListenerHostedServiceShould
    {
        private readonly MessageListenerHostedService<MyEndpointHandlingConfig> _listenerHostedService;
        private readonly Mock<IMessageListener<MyEndpointHandlingConfig>> _mockListener;

        public MessageListenerHostedServiceShould()
        {
            _mockListener = new Mock<IMessageListener<MyEndpointHandlingConfig>>();

            _listenerHostedService = new MessageListenerHostedService<MyEndpointHandlingConfig>(
                new NullLogger<MessageListenerHostedService<MyEndpointHandlingConfig>>(),
                _mockListener.Object);
        }

        [Fact]
        public async Task Start_Listening_On_Start()
        {
            await _listenerHostedService.StartAsync(new CancellationToken());

            _mockListener.Verify(l => l.StartListeningAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Stop_Listening_On_Stop()
        {
            await _listenerHostedService.StopAsync(new CancellationToken());

            _mockListener.Verify(l => l.StopListeningAsync(), Times.Once);
        }
    }
}
