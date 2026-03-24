using PayFlow.OrdersApi.Domain;

namespace PayFlow.OrdersApi.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId);
    Task SaveAsync(Order order);
}
