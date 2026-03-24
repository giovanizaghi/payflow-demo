using PayFlow.OrdersApi.Domain;

namespace PayFlow.OrdersApi.Repositories;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = new();

    public Task<Order?> GetByIdAsync(Guid orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId)
    {
        var result = _orders.Values
            .Where(o => o.CustomerId == customerId)
            .ToList();
        return Task.FromResult<IReadOnlyList<Order>>(result);
    }

    public Task SaveAsync(Order order)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
