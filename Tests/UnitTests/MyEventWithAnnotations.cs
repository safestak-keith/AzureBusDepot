using Newtonsoft.Json;

namespace AzureBusDepot.UnitTests
{
    public class MyEventWithAnnotations
    {
        [JsonProperty (Required = Required.Always)]
        public int Id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
     
    }
}
