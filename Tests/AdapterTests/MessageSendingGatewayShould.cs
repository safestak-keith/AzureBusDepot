using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace AzureBusDepot.AdapterTests
{
    public class MessageSendingGatewayShould
    {
        private readonly MessageSendingGateway<MyEndpointHandlingConfig> _outboundGateway;
        private readonly MyEndpointHandlingConfig _sendingOptions = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.sendinggateway.myevent"
        };
        private readonly MyEndpointHandlingConfig _receivingOptions = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.sendinggateway.myevent/Subscriptions/MessageSendingGatewayShould"
        };

        public MessageSendingGatewayShould()
        {
            _outboundGateway = new MessageSendingGateway<MyEndpointHandlingConfig>(
                new NullLogger<MessageSendingGateway<MyEndpointHandlingConfig>>(),
                new JsonMessageSerialiser(new NullLogger<JsonMessageSerialiser>()),
                new VoidInstrumentor(),
                new MessageSenderFactory(),
                _sendingOptions);
        }

        [Theory(Skip = "Only to be run locally (with correct connection string)")]
        [InlineData(5)]
        public async Task Send_Message_Which_Can_Be_Received_And_Deserialised(int id)
        {
            var messageEntity = new MyEvent { Id = id, Name = $"Hello {id}" };
            var messageProps = new Dictionary<string, object>
            {
                {"CustomProperty", "Hello"}
            };

            await _outboundGateway.SendAsync(messageEntity, messageProps).ConfigureAwait(false);
            await Task.Delay(3000).ConfigureAwait(false);

            var receivingValidator = new MessageReceiver(_receivingOptions.ConnectionString, _receivingOptions.EntityPath, ReceiveMode.ReceiveAndDelete);
            var receivedMessage = (await receivingValidator.ReceiveAsync(5).ConfigureAwait(false)).FirstOrDefault();
            receivedMessage.Should().NotBeNull();
            var deserialisedMessageEntity = JsonConvert.DeserializeObject<MyEvent>(Encoding.UTF8.GetString(receivedMessage.Body));
            deserialisedMessageEntity.Id.Should().Be(id);
            deserialisedMessageEntity.Name.Should().Be(messageEntity.Name);
        }
    }
}
