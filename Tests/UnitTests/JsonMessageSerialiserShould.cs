using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using AzureBusDepot.Exceptions;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace AzureBusDepot.UnitTests
{
    public class JsonMessageSerialiserShould
    {
        private readonly JsonMessageSerialiser _jsonSerialiser;
        private readonly Mock<ILogger<JsonMessageSerialiser>> _mockLogger;

        public JsonMessageSerialiserShould()
        {
            _mockLogger = new Mock<ILogger<JsonMessageSerialiser>>();
            _jsonSerialiser = new JsonMessageSerialiser(_mockLogger.Object);
        }

        [Fact]
        public void Throw_ArgumentNullException_Given_Message_Is_Null_When_Deserialising()
        {
            Action x = () => _jsonSerialiser.Deserialise<MyEvent>(null);
            x.Should().ThrowExactly<ArgumentNullException>();
        }

        [Theory]
        [InlineData("{\"Id\": 5, \"Name\":\"Hello world\"}", 5, "Hello world")]
        [InlineData("{\"Id\": 573124, \"Name\":\"573124\"}", 573124, "573124")]
        public void Deserialise_Message_Given_Valid_Message_When_Deserialising(string input, int expectedId, string expectedName)
        {
            var result = _jsonSerialiser.Deserialise<MyEvent>(NewMessageWithBody(input));

            result.Id.Should().Be(expectedId);
            result.Name.Should().Be(expectedName);
        }

        [Theory]
        [InlineData("{\"Id\": 5, \"Name\":\"Hello world\"}", 5, "Hello world")]
        [InlineData("{\"Id\": 573124, \"Name\":\"573124\"}", 573124, "573124")]
        public void Deserialise_Message_Given_Message_Serialised_With_Older_Sdks_Or_XmlSerialiser_When_Deserialising(string input, int expectedId, string expectedName)
        {
            var result = _jsonSerialiser.Deserialise<MyEvent>(NewMessageWithBodySerialisedWithXmlSerialiser(input));

            result.Id.Should().Be(expectedId);
            result.Name.Should().Be(expectedName);
        }

        [Theory]
        [InlineData("{\"Id\": 5, \"Name\":\"Hello world\"}", 5, "Hello world")]
        [InlineData("{\"Id\": 573124, \"Name\":\"573124\"}", 573124, "573124")]
        public void Deserialise_Message_Given_Message_Body_Is_Null_But_SystemProperties_BodyObject_Is_String(string input, int expectedId, string expectedName)
        {
            var message = NewMessageWithBody(input);
            message.Body = null;

            var messageSystemPropertiesType = message.SystemProperties.GetType();
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                       | BindingFlags.Static;
            var property = messageSystemPropertiesType.GetProperty("BodyObject", bindingAttr);
            property.SetValue(message.SystemProperties, input);
            
            var result = _jsonSerialiser.Deserialise<MyEvent>(message);

            result.Id.Should().Be(expectedId);
            result.Name.Should().Be(expectedName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("?")]
        [InlineData("Hello world")]
        public void Throw_MessageSerialisationException_With_InvalidJson_When_Deserialising(string invalidJsonForType)
        {
            Assert.Throws<MessageSerialisationException>(() =>
            {
                _jsonSerialiser.Deserialise<MyEvent>(NewMessageWithBody(invalidJsonForType));
            });
        }

        [Theory]
        [InlineData("{\"Id\": [1,2], \"Bar\":\"Hello world\"}")]
        [InlineData("{\"Id\":, \"Bar\":\"Hello world\"}")]
        [InlineData("[]")]
        public void Throw_MessageSerialisationException_On_ValidJson_When_Deserialising_To_Type_That_Has_Constraints(string invalidJsonForType)
        {
            Assert.Throws<MessageSerialisationException>(() =>
            {
                _jsonSerialiser.Deserialise<MyEvent>(NewMessageWithBody(invalidJsonForType));
            });
        }

        [Fact]
        public void Throw_MessageSerialisationException_When_Serializing_Constrained_Type_With_Values_Not_Filled_In()
        {
            var myEvent= new MyEventWithAnnotations(); // explicitly not setting any property

            var ex = Assert.Throws<MessageSerialisationException>(() => _jsonSerialiser.Serialise(myEvent));

            ex.InnerException.Should().BeOfType<JsonSerializationException>();
        }

        [Fact]
        public void Throw_ArgumentNullException_Given_Message_Is_Null_When_Serialising()
        {
            Action x = () => _jsonSerialiser.Serialise((MyEvent)null);
            x.Should().ThrowExactly<ArgumentNullException>();
        }

        [Theory]
        [InlineData(5, "Hello world", "{\"Id\":5,\"Name\":\"Hello world\"}")]
        [InlineData(573124, "573124", "{\"Id\":573124,\"Name\":\"573124\"}")]
        public void Serialise_Message_Given_Valid_Message_When_Deserialising(int inputId, string inputName, string expectedOutput)
        {
            var result = _jsonSerialiser.Serialise(new MyEvent{ Id=inputId, Name=inputName});

            result.Should().BeEquivalentTo(Encoding.UTF8.GetBytes(expectedOutput));
        }

        private static Message NewMessageWithBody(string body)
        {
            return new Message(Encoding.UTF8.GetBytes(body));
        }

        private static Message NewMessageWithBodySerialisedWithXmlSerialiser(string body)
        {
            var serializer = new DataContractSerializer(typeof(string));
            var memoryStream = new MemoryStream();
            var binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(memoryStream);
            serializer.WriteObject(binaryDictionaryWriter, body);
            binaryDictionaryWriter.Flush();
            return new Message(memoryStream.ToArray());
        }
    }
}
