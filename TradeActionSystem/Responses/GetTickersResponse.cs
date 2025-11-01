using TradeActionSystem;
using System.Text.Json.Serialization;

public class GetTickersResponse : BaseResponse
{
    [JsonPropertyName("Tickers")]
    public IList<string> Tickers { get; }
    public GetTickersResponse(bool success, string message, IList<string> tickers) : base(success, message)
    {
        Tickers = tickers;
    }
}

