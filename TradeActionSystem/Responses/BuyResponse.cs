using System.Text.Json.Serialization;
using TradeActionSystem;

public class BuyResponse : BaseResponse
{
    [JsonPropertyName("Prices")]
    public IDictionary<string, decimal> Prices { get; }
    public BuyResponse(bool success, string message, IDictionary<string, decimal> prices) : base(success, message)
    {
        Prices = prices;
    }
}

