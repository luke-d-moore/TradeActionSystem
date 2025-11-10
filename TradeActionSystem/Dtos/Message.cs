using System.Text.Json.Serialization;

namespace TradeActionSystem.Dtos
{
    public class Message
    {
        [JsonPropertyName("Ticker")]
        public string Ticker { get; set; }
        [JsonPropertyName("Quantity")]
        public int Quantity { get; set; }
        [JsonPropertyName("Action")]
        public string Action { get; set; }
        public Message(string ticker, int quantity, string action)
        {
            Ticker = ticker;
            Quantity = quantity;
            Action = action;
        }
    }
}
