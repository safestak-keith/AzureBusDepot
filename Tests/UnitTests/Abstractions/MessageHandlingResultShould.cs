using System.Collections.Generic;
using AzureBusDepot.Abstractions;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Xunit;

namespace AzureBusDepot.UnitTests.Abstractions
{
    public class MessageHandlingResultShould
    {
        [Theory]
        [InlineData("JsonSerialisationException thrown", "CorrelationId", "1")]
        [InlineData("MessageType mapping not found", "MessageId", "2")]
        public void Return_Expected_Result_Using_UnrecognisedMessageType_Helper_Given_Details_And_AdditionalProperties(
            string details, string additionalPropertyKey, string additionalPropertyValue)
        {
            var additionalProperties = new Dictionary<string, object>
            {
                {additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.UnrecognisedMessageType(details, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.UnrecognisedMessageType);
            result.AdditionalProperties["AzureBusDepot.UnrecognisedMessageType"].Should().Be(details);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(2);
        }

        [Theory]
        [InlineData("JsonSerialisationException thrown", "CorrelationId", "1")]
        [InlineData("MessageType mapping not found", "MessageId", "2")]
        public void Return_Expected_Result_Using_UnrecognisedMessageType_Helper_Given_Details_And_AdditionalProperties_And_Reserved_Property_Keys_Already_Exist(
            string details, string additionalPropertyKey, string additionalPropertyValue)
        {
            var additionalProperties = new Dictionary<string, object>
            {
                {"AzureBusDepot.UnrecognisedMessageType", "existing" },
                { additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.UnrecognisedMessageType(details, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.UnrecognisedMessageType);
            result.AdditionalProperties["AzureBusDepot.UnrecognisedMessageType"].Should().Be(details);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(2);
        }

        [Theory]
        [InlineData("CorrelationId", "1")]
        [InlineData("MessageId", "2")]
        public void Return_Expected_Result_Using_CompletedMessageType_Helper_Given_AdditionalProperties(
            string additionalPropertyKey, string additionalPropertyValue)
        {
            var additionalProperties = new Dictionary<string, object>
            {
                {additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.Completed(additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.Completed);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(1);
        }

        [Theory]
        [InlineData("JsonSerialisationException thrown", "CorrelationId", "1")]
        [InlineData("MessageType mapping not found", "MessageId", "2")]
        public void Return_Expected_Result_Using_DeadLetteredMessageType_Helper_Given_Details_And_AdditionalProperties(
            string details, string additionalPropertyKey, string additionalPropertyValue)
        {
            var additionalProperties = new Dictionary<string, object>
            {
                {additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.DeadLettered(details, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.DeadLettered);
            result.AdditionalProperties["AzureBusDepot.DeadLettered"].Should().Be(details);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(2);
        }

        [Theory]
        [InlineData("JsonSerialisationException thrown", "CorrelationId", "1")]
        [InlineData("MessageType mapping not found", "MessageId", "2")]
        public void Return_Expected_Result_Using_DeadLetteredMessageType_Helper_Given_Details_And_AdditionalProperties_And_Reserved_Property_Keys_Already_Exist(
            string details, string additionalPropertyKey, string additionalPropertyValue)
        {
            var additionalProperties = new Dictionary<string, object>
            {
                { "AzureBusDepot.DeadLettered", "existing" },
                { additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.DeadLettered(details, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.DeadLettered);
            result.AdditionalProperties["AzureBusDepot.DeadLettered"].Should().Be(details);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(2);
        }

        [Theory]
        [InlineData("CorrelationId", "1")]
        [InlineData("MessageId", "2")]
        public void Return_Expected_Result_Using_DeadLetteredMessageType_Helper_Given_Exception_And_AdditionalProperties(
            string additionalPropertyKey, string additionalPropertyValue)
        {
            var exception = new MessageSizeExceededException("That's no moon");
            var additionalProperties = new Dictionary<string, object>
            {
                {additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.DeadLettered(exception, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.DeadLettered);
            result.AdditionalProperties["AzureBusDepot.DeadLettered"].Should().Be("Exception");
            result.AdditionalProperties["AzureBusDepot.Exception.Message"].Should().Be(exception.Message);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(4);
        }

        [Theory]
        [InlineData("CorrelationId", "1")]
        [InlineData("MessageId", "2")]
        public void Return_Expected_Result_Using_DeadLetteredMessageType_Helper_Given_Exception_And_AdditionalProperties_And_Reserved_Property_Keys_Already_Exist(
            string additionalPropertyKey, string additionalPropertyValue)
        {
            var exception = new MessageSizeExceededException("That's no moon");
            var additionalProperties = new Dictionary<string, object>
            {
                {"AzureBusDepot.DeadLettered", "existing" },
                {"AzureBusDepot.Exception.Message", "existing" },
                { additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.DeadLettered(exception, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.DeadLettered);
            result.AdditionalProperties["AzureBusDepot.DeadLettered"].Should().Be("Exception");
            result.AdditionalProperties["AzureBusDepot.Exception.Message"].Should().Be(exception.Message);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(4);
        }

        [Theory]
        [InlineData("JsonSerialisationException thrown", "CorrelationId", "1")]
        [InlineData("MessageType mapping not found", "MessageId", "2")]
        public void Return_Expected_Result_Using_AbandonedMessageType_Helper_Given_Details_And_AdditionalProperties(
            string details, string additionalPropertyKey, string additionalPropertyValue)
        {
            var additionalProperties = new Dictionary<string, object>
            {
                {additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.Abandoned(details, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.Abandoned);
            result.AdditionalProperties["AzureBusDepot.Abandoned"].Should().Be(details);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(2);
        }

        [Theory]
        [InlineData("JsonSerialisationException thrown", "CorrelationId", "1")]
        [InlineData("MessageType mapping not found", "MessageId", "2")]
        public void Return_Expected_Result_Using_AbandonedMessageType_Helper_Given_Details_And_AdditionalProperties_And_Reserved_Property_Keys_Already_Exist(
            string details, string additionalPropertyKey, string additionalPropertyValue)
        {
            var additionalProperties = new Dictionary<string, object>
            {
                {"AzureBusDepot.Abandoned", "existing" },
                { additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.Abandoned(details, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.Abandoned);
            result.AdditionalProperties["AzureBusDepot.Abandoned"].Should().Be(details);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(2);
        }

        [Theory]
        [InlineData("CorrelationId", "1")]
        [InlineData("MessageId", "2")]
        public void Return_Expected_Result_Using_AbandonedMessageType_Helper_Given_Exception_And_AdditionalProperties(
            string additionalPropertyKey, string additionalPropertyValue)
        {
            var exception = new MessageSizeExceededException("That's no moon");
            var additionalProperties = new Dictionary<string, object>
            {
                {additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.Abandoned(exception, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.Abandoned);
            result.AdditionalProperties["AzureBusDepot.Abandoned"].Should().Be("Exception");
            result.AdditionalProperties["AzureBusDepot.Exception.Message"].Should().Be(exception.Message);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(4);
        }

        [Theory]
        [InlineData("CorrelationId", "1")]
        [InlineData("MessageId", "2")]
        public void Return_Expected_Result_Using_AbandonedMessageType_Helper_Given_Exception_And_AdditionalProperties_And_AdditionalProperties_And_Reserved_Property_Keys_Already_Exist(
            string additionalPropertyKey, string additionalPropertyValue)
        {
            var exception = new MessageSizeExceededException("That's no moon");
            var additionalProperties = new Dictionary<string, object>
            {
                {"AzureBusDepot.Abandoned", "existing" },
                {"AzureBusDepot.Exception.Message", "existing" },
                { additionalPropertyKey, additionalPropertyValue}
            };
            var result = MessageHandlingResult.Abandoned(exception, additionalProperties);

            result.Result.Should().Be(MessageHandlingResult.HandlingResult.Abandoned);
            result.AdditionalProperties["AzureBusDepot.Abandoned"].Should().Be("Exception");
            result.AdditionalProperties["AzureBusDepot.Exception.Message"].Should().Be(exception.Message);
            result.AdditionalProperties[additionalPropertyKey].Should().Be(additionalPropertyValue);
            result.AdditionalProperties.Count.Should().Be(4);
        }
    }
}