# Bug #2 — Sequence Diagram: Order Cancellation & Points Reversal

```mermaid
sequenceDiagram
    participant Client
    participant OrdersController
    participant OrderService
    participant EventBus as InMemoryEventBus
    participant PointsService
    participant PointsRepository

    %% ─── Happy Path: Place Order ────────────────────────────────────────────

    Note over Client,PointsRepository: Happy Path — Place Order

    Client->>OrdersController: POST /api/orders
    OrdersController->>OrderService: PlaceOrderAsync(customerId, total)
    OrderService->>OrderService: order.Confirm()
    OrderService->>EventBus: PublishAsync(OrderPlacedEvent)
    EventBus->>PointsService: ✅ EarnForOrderAsync(customerId, orderId, total)
    PointsService->>PointsRepository: GetOrCreateAccountAsync
    PointsService->>PointsRepository: SaveAccountAsync (balance += points)
    PointsService->>PointsRepository: RecordTransactionAsync (type: "earn")
    PointsService->>EventBus: PublishAsync(PointsEarnedEvent)
    OrdersController-->>Client: 201 Created

    %% ─── Bug Path: Cancel Order ─────────────────────────────────────────────

    Note over Client,PointsRepository: Cancel Order — Bug #2

    Client->>OrdersController: DELETE /api/orders/{orderId}
    OrdersController->>OrderService: CancelOrderAsync(orderId)
    OrderService->>OrderService: order.Cancel()
    OrderService->>EventBus: PublishAsync(OrderCancelledEvent)

    Note over EventBus,PointsService: ❌ No Subscribe<OrderCancelledEvent> registered in PointsApi/Program.cs

    EventBus--xPointsService: OrderCancelledEvent silently dropped (no handler)

    Note over PointsService: ❌ ReverseForOrderAsync is never called

    Note over PointsRepository: ❌ Balance unchanged — customer keeps points from a cancelled order

    OrderService-->>OrdersController: CancelOrderResult(Success: true)
    OrdersController-->>Client: 204 No Content
```

## Gap summary

| Step | Status | Location |
|---|---|---|
| `OrderPlacedEvent` → `EarnForOrderAsync` subscription | ✅ wired | `src/PayFlow.Host/Program.cs` or `src/PayFlow.OrdersApi/Program.cs` |
| `OrderCancelledEvent` published on cancellation | ✅ implemented | `src/PayFlow.OrdersApi/Services/OrderService.cs` |
| `OrderCancelledEvent` → `ReverseForOrderAsync` subscription | ❌ **missing** | `PayFlow.PointsApi/Program.cs` |
| `ReverseForOrderAsync` implementation | ✅ implemented | `PayFlow.PointsApi/Services/PointsService.cs` |
