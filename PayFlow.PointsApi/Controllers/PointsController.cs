using Microsoft.AspNetCore.Mvc;
using PayFlow.PointsApi.Services;

namespace PayFlow.PointsApi.Controllers;

[ApiController]
[Route("api/points")]
public class PointsController : ControllerBase
{
    private readonly IPointsService _pointsService;

    public PointsController(IPointsService pointsService)
    {
        _pointsService = pointsService;
    }

    [HttpGet("{customerId:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid customerId)
    {
        var balance = await _pointsService.GetBalanceAsync(customerId);
        return Ok(new { customerId, balance });
    }

    [HttpPost("{customerId:guid}/earn")]
    public async Task<IActionResult> Earn(Guid customerId, [FromBody] EarnRequest request)
    {
        var result = await _pointsService.EarnForOrderAsync(customerId, request.OrderId, request.OrderTotal);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{customerId:guid}/redeem")]
    public async Task<IActionResult> Redeem(Guid customerId, [FromBody] RedeemRequest request)
    {
        var result = await _pointsService.RedeemAsync(customerId, request.Points, request.OrderId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{customerId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid customerId)
    {
        var transactions = await _pointsService.GetTransactionsAsync(customerId);
        return Ok(transactions);
    }
}

public record EarnRequest(Guid OrderId, decimal OrderTotal);
public record RedeemRequest(Guid OrderId, int Points);
