using System.Text.Json.Serialization;

namespace TradeActionSystem.Dtos
{
    public class Message
    {
        [JsonPropertyName("Ticker")]
        public string Ticker { get; set; }
        [JsonPropertyName("UniqueID")]
        public string UniqueID { get; set; }
        [JsonPropertyName("Quantity")]
        public int Quantity { get; set; }
        [JsonPropertyName("Action")]
        public string Action { get; set; }
    }
}
