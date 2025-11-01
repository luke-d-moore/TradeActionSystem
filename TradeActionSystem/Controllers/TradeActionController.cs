using TradeActionSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Mvc.Routing;

namespace PricingSyetem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradeActionController : ControllerBase
    {
        private readonly ILogger<TradeActionController> _logger;
        private readonly ITradeActionService _tradeActionService;

        public TradeActionController(ILogger<TradeActionController> logger, ITradeActionService tradeActionService)
        {
            _logger = logger;
            _tradeActionService = tradeActionService;
        }

        //// GET: api/<PriceController>
        //[HttpGet(nameof(GetPrice) + "/{ticker}")]
        //[SwaggerOperation(nameof(GetPrice))]
        //[SwaggerResponse(StatusCodes.Status200OK, "OK")]
        //public async Task<IActionResult> GetPrice(string ticker)
        //{
        //    try
        //    {
        //        var price = await _pricingService.GetCurrentPrice(ticker);
        //        var response = new GetPriceResponse(true, "Price Retrieved", new Dictionary<string, decimal>() { { ticker, price } });
        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(
        //            title: "Price Retrieve Failed",
        //            detail: ex.Message,
        //            statusCode: StatusCodes.Status404NotFound
        //        );
        //    }
        //}

        //// GET: api/<PriceController>
        //[HttpGet]
        //[Route(nameof(GetAllPrices))]
        //[SwaggerOperation(nameof(GetAllPrices))]
        //[SwaggerResponse(StatusCodes.Status200OK, "OK")]
        //public IActionResult GetAllPrices()
        //{
        //    var tickers = _pricingService.GetPrices();
        //    var response = new GetPriceResponse(true, "Prices Retrieved", tickers);
        //    return Ok(response);
        //}

        //// GET: api/<PriceController>
        //[HttpGet]
        //[Route(nameof(GetTickers))]
        //[SwaggerOperation(nameof(GetTickers))]
        //[SwaggerResponse(StatusCodes.Status200OK, "OK")]
        //public IActionResult GetTickers()
        //{
        //    var tickers = _pricingService.GetTickers();
        //    var response = new GetTickersResponse(true, "Tickers Retrieved", tickers);
        //    return Ok(response);
        //}
    }
}
