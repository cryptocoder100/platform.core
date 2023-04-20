#pragma warning disable CA2227 // Collection properties should be read only
namespace AzureMessagingTest.Testmodel
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json.Serialization;
    using Exos.Platform.Messaging.Repository.Model;
    using Newtonsoft.Json;

    [ExcludeFromCodeCoverage]
    public class TestData
    {
        [JsonProperty("MessageEntities")]
        [JsonPropertyName("MessageEntities")]
        public IList<MessageEntity> MessageEntities { get; set; }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
