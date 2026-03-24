# Reproducing Bug #2 — Cancelled orders don't reverse points

## What the bug is

`PayFlow.OrdersApi` publishes `OrderCancelledEvent` when an order is cancelled.
`PayFlow.PointsApi` has a fully-implemented `ReverseForOrderAsync` method but
**no subscription** to `OrderCancelledEvent` — so the method is never called.

Result: points earned for a cancelled order are permanently credited to the customer.

## Setup

```bash
# Option A — Docker
docker compose up --build

# Option B — direct run
cd src/PayFlow.Host && dotnet run
```

Both expose all endpoints on **http://localhost:8080**.

---

## Step-by-step reproduction

### Step 1 — Place an order (triggers automatic point earning)

```bash
curl -s -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","total":200}' \
  | jq .
```

**Expected response (201 Created):**
```json
{
  "id": "<orderId>",
  "customerId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "total": 200,
  "status": 1
}
```

Copy the returned `id` as `<orderId>`.

*Behind the scenes:* `OrderPlacedEvent` is published → `PointsService.EarnForOrderAsync` runs → **200 points credited**.

---

### Step 2 — Verify points were earned

```bash
curl -s http://localhost:8080/api/points/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/balance \
  | jq .
```

**Expected response:**
```json
{ "balance": 200 }
```

---

### Step 3 — Cancel the order

```bash
curl -s -X DELETE http://localhost:8080/api/orders/<orderId> | jq .
```

**Expected response:**
```json
{ "message": "Order cancelled" }
```

*Behind the scenes:* `OrderCancelledEvent` is published by `OrderService`. But **nobody is subscribed** — `ReverseForOrderAsync` is never called.

---

### Step 4 — Check balance again (BUG visible here)

```bash
curl -s http://localhost:8080/api/points/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/balance \
  | jq .
```

**Actual response:**
```json
{ "balance": 200 }
```

**Expected after fix:**
```json
{ "balance": 0 }
```

**← BUG PROVEN: balance is still 200 after cancellation.**

---

## Fix

In [src/PayFlow.Host/Program.cs](../PayFlow.Host/Program.cs), find the `BUG #2` comment block and uncomment the `OrderCancelledEvent` subscription:

```csharp
eventBus.Subscribe<OrderCancelledEvent>(async e =>
{
    using var scope = scopeFactory.CreateScope();
    var pointsService = scope.ServiceProvider.GetRequiredService<IPointsService>();
    await pointsService.ReverseForOrderAsync(e.CustomerId, e.OrderId);
});
```

Restart the application and repeat steps 1–4.

**Result after fix:**
- Step 2 → `{ "balance": 200 }` ✅
- Step 4 → `{ "balance": 0 }` ✅

---

## Key files

| File | Role |
|------|------|
| `src/PayFlow.OrdersApi/Services/OrderService.cs` | Publishes `OrderCancelledEvent` |
| `PayFlow.PointsApi/Services/PointsService.cs` | Implements `ReverseForOrderAsync` (works correctly) |
| `src/PayFlow.Host/Program.cs` | **Missing subscription** (Bug #2 location) |
| `src/PayFlow.Shared/Events/DomainEvents.cs` | `OrderCancelledEvent` definition |
