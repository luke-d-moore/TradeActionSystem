using TradeActionSystem;
using System.Text.Json.Serialization;

public class GetPriceResponse : BaseResponse
{
    [JsonPropertyName("Prices")]
    public IDictionary<string, decimal> Prices { get; }
    public GetPriceResponse(bool success, string message, IDictionary<string, decimal> prices) : base(success, message)
    {
        Prices = prices;
    }
}

