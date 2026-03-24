using PayFlow.OrdersApi.Domain;
using PayFlow.OrdersApi.Repositories;
using PayFlow.Shared.Events;
using PayFlow.Shared.Messaging;

namespace PayFlow.OrdersApi.Services;

public record PlaceOrderResult(bool Success, Order? Order, string? Error = null);
public record CancelOrderResult(bool Success, string? Error = null);

public interface IOrderService
{
    Task<PlaceOrderResult> PlaceOrderAsync(Guid customerId, decimal total);
    Task<CancelOrderResult> CancelOrderAsync(Guid orderId);
    Task<Order?> GetOrderAsync(Guid orderId);
    Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(Guid customerId);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repository, IEventBus eventBus, ILogger<OrderService> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(Guid customerId, decimal total)
    {
        var order = Order.Create(customerId, total);
        order.Confirm();
        await _repository.SaveAsync(order);

        await _eventBus.PublishAsync(new OrderPlacedEvent(
            OrderId: order.Id,
            CustomerId: order.CustomerId,
            OrderTotal: order.Total,
            CurrencyCode: order.CurrencyCode,
            PlacedAt: order.PlacedAt));

        return new PlaceOrderResult(true, order);
    }

    public async Task<CancelOrderResult> CancelOrderAsync(Guid orderId)
    {
        var order = await _repository.GetByIdAsync(orderId);
        if (order is null)
            return new CancelOrderResult(false, "Order not found");

        try
        {
            order.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return new CancelOrderResult(false, ex.Message);
        }

        await _repository.SaveAsync(order);

        await _eventBus.PublishAsync(new OrderCancelledEvent(
            OrderId: order.Id,
            CustomerId: order.CustomerId,
            OrderTotal: order.Total,
            CancelledAt: order.CancelledAt!.Value));

        // BUG #2: OrderCancelledEvent is published correctly here.
        // However, PayFlow.PointsApi has no subscriber for this event.
        // Points earned for this order will NOT be reversed.
        // See docs/bug-analysis/bug-02-cancellation-gap.md

        return new CancelOrderResult(true);
    }

    public Task<Order?> GetOrderAsync(Guid orderId) =>
        _repository.GetByIdAsync(orderId);

    public Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(Guid customerId) =>
        _repository.GetByCustomerIdAsync(customerId);
}
