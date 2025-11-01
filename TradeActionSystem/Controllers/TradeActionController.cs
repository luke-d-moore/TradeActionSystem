using TradeActionSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Mvc.Routing;

namespace TradeAction.Controllers
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

        // GET: api/<PriceController>
        [HttpGet(nameof(Buy))]
        [SwaggerOperation(nameof(Buy))]
        [SwaggerResponse(StatusCodes.Status200OK, "OK")]
        public async Task<IActionResult> Buy()
        {
            try
            {
                //make a new request object to pass into the controller which we can take values from for use in the service call
                var price = await _tradeActionService.BuyAsync("IBM",10,10);
                //var response = new GetPriceResponse(true, "Price Retrieved", new Dictionary<string, decimal>() { { ticker, price } });
                //return Ok(response);
                //make new response object for this api call
                return Ok(price);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Buy Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound
                );
            }
        }

        // GET: api/<PriceController>
        [HttpGet]
        [Route(nameof(Sell))]
        [SwaggerOperation(nameof(Sell))]
        [SwaggerResponse(StatusCodes.Status200OK, "OK")]
        public async Task<IActionResult> Sell()
        {
            try
            {
                //make a new request object to pass into the controller which we can take values from for use in the service call
                var price = await _tradeActionService.SellAsync("IBM", 10, 0);
                //var response = new GetPriceResponse(true, "Price Retrieved", new Dictionary<string, decimal>() { { ticker, price } });
                //return Ok(response);
                //make new response object for this api call
                return Ok(price);
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Sell Failed",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound
                );
            }
        }
    }
}
