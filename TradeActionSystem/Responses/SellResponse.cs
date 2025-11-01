using TradeActionSystem;
using System.Text.Json.Serialization;

public class SellResponse : BaseResponse
{
    [JsonPropertyName("Tickers")]
    public IList<string> Tickers { get; }
    public SellResponse(bool success, string message, IList<string> tickers) : base(success, message)
    {
        Tickers = tickers;
    }
}

