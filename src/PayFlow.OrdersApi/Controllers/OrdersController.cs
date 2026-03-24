using Microsoft.AspNetCore.Mvc;
using PayFlow.OrdersApi.Services;

namespace PayFlow.OrdersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var result = await _orderService.PlaceOrderAsync(request.CustomerId, request.Total);
        if (!result.Success)
            return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(GetOrder), new { orderId = result.Order!.Id }, result.Order);
    }

    [HttpDelete("{orderId:guid}")]
    public async Task<IActionResult> CancelOrder(Guid orderId)
    {
        var result = await _orderService.CancelOrderAsync(orderId);
        if (!result.Success)
        {
            if (result.Error == "Order not found")
                return NotFound(new { result.Error });
            return BadRequest(new { result.Error });
        }
        return Ok(new { message = "Order cancelled" });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var order = await _orderService.GetOrderAsync(orderId);
        if (order is null)
            return NotFound();
        return Ok(order);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetOrdersByCustomer(Guid customerId)
    {
        var orders = await _orderService.GetOrdersByCustomerAsync(customerId);
        return Ok(orders);
    }
}

public record PlaceOrderRequest(Guid CustomerId, decimal Total);
