namespace PayFlow.OrdersApi.Domain;

public enum OrderStatus { Pending, Confirmed, Cancelled, Refunded }

public class Order
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public decimal Total { get; private set; }
    public string CurrencyCode { get; private set; } = "BRL";
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public DateTimeOffset PlacedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CancelledAt { get; private set; }

    private Order() { }

    public static Order Create(Guid customerId, decimal total) => new()
    {
        CustomerId = customerId,
        Total = total
    };

    public void Confirm() => Status = OrderStatus.Confirmed;

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled.");
        Status = OrderStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
    }
}
