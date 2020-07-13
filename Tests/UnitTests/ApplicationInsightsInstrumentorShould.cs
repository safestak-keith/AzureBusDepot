using System.Collections.Generic;
using AzureBusDepot.ApplicationInsights;
using FluentAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using Moq;
using Xunit;

namespace AzureBusDepot.UnitTests
{
    public class ApplicationInsightsInstrumentorShould
    {
        private readonly int _eventId = 45246;

        [Fact]
        public void Not_Add_Event_Id_Property_If_Telemetry_Properties_Not_Supplied()
        {
            var supportPropertiesMock = new Mock<ISupportProperties>();

            supportPropertiesMock.Setup(m => m.Properties).Returns((IDictionary<string, string>) null);

            ApplicationInsightsInstrumentor.EnrichTelemetryProperties(_eventId, null, supportPropertiesMock.Object);

            supportPropertiesMock.Verify(d => d.Properties.Add(It.IsAny<KeyValuePair<string, string>>()), Times.Never);
        }

        [Fact]
        public void Add_Event_Id_If_Telemetry_Properties_Supplied()
        {
            var supportProperties = new RequestTelemetry();

            ApplicationInsightsInstrumentor.EnrichTelemetryProperties(_eventId, null, supportProperties);

            supportProperties.Properties.Count.Should().Be(1);
            supportProperties.Properties.TryGetValue("EventId", out var eventIdValue);
            eventIdValue.Should().Be(_eventId.ToString());
        }

        [Theory]
        [InlineData("CustomPropertyKeyString", "CustomPropertyValue", "CustomPropertyValue")]
        [InlineData("CustomPropertyKeyNull", null, null)]
        [InlineData("CustomPropertyKeyInt", 5435, "5435")]
        [InlineData("CustomPropertyKeyBool", true, "True")]
        public void Add_Additional_Telemetry_Properties_If_Custom_Properties_Supplied(string customPropertyKey, object customPropertyValue, string expectedCustomPropertyValue)
        {
            var supportProperties = new RequestTelemetry();

            var customProperties = new Dictionary<string, object>
            {
                { customPropertyKey, customPropertyValue }
            };

            ApplicationInsightsInstrumentor.EnrichTelemetryProperties(_eventId, customProperties, supportProperties);

            supportProperties.Properties.Count.Should().Be(2);
            supportProperties.Properties.TryGetValue("EventId", out var eventIdValue);
            eventIdValue.Should().Be(_eventId.ToString());

            supportProperties.Properties.TryGetValue(customPropertyKey, out var result);
            result.Should().Be(expectedCustomPropertyValue);
        }
    }
}
