# PayFlow Demo

A multi-service e-commerce demo built with .NET 9 and ASP.NET Core.  
The project is intentionally designed to contain two documented bugs, making it useful for live demos, debugging workshops, and teaching event-driven architecture pitfalls.

## Architecture

```
PayFlow.Host          ← single ASP.NET Core process hosting both APIs
├── PayFlow.OrdersApi ← order management (place, cancel, list)
├── PayFlow.PointsApi ← loyalty points (earn, redeem, reverse)
└── PayFlow.Shared    ← shared contracts: domain events + IEventBus
```

Both APIs share **one in-memory event bus singleton** inside `PayFlow.Host`, enabling cross-service communication without a message broker.

## Intentional Bugs

| Bug | Location | Description |
|-----|----------|-------------|
| **Bug #1** | `PointsAccount.Earn()` | Race condition on monthly earning cap — concurrent requests can each read a stale `EarnedThisMonth` and both award points, exceeding the 500-point cap. Exposed by a failing unit test. |
| **Bug #2** | `PayFlow.Host/Program.cs` | Missing `OrderCancelledEvent` subscription — points earned for a cancelled order are never reversed. `ReverseForOrderAsync` is fully implemented but never called. See [docs/bug-analysis/REPRODUCE-BUG-02.md](docs/bug-analysis/REPRODUCE-BUG-02.md). |

## How to Run

### Option A — Docker (recommended)

```bash
docker compose up --build
```

### Option B — Direct

```bash
cd src/PayFlow.Host
dotnet run
```

Both options expose everything on **http://localhost:8080**.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | /health | Host health — lists both APIs |
| POST | /api/orders | Place an order (auto-earns points via event) |
| DELETE | /api/orders/{id} | Cancel an order (publishes event, points NOT reversed ← Bug #2) |
| GET | /api/orders/{id} | Get order by ID |
| GET | /api/orders/customer/{id} | List orders by customer |
| GET | /api/points/{id}/balance | Get points balance |
| POST | /api/points/{id}/earn | Manually earn points |
| POST | /api/points/{id}/redeem | Redeem points |
| GET | /api/points/{id}/transactions | List points transactions |

## Swagger UI

```
http://localhost:8080/swagger
```

Two grouped documents: **PayFlow Points API** and **PayFlow Orders API**.

## Running Tests

```bash
dotnet test tests/PayFlow.Unit.Tests
```

19 tests pass. 1 test intentionally fails — `EarnForOrder_ConcurrentRequests_ShouldRespectMonthlyCap_ButDoesNot` — to demonstrate Bug #1.

## Project Structure

```
payflow-demo/
├── PayFlow.PointsApi/          # Points microservice (at repo root)
├── src/
│   ├── PayFlow.Host/           # Combined host — single entry point
│   ├── PayFlow.OrdersApi/      # Orders microservice
│   └── PayFlow.Shared/         # Shared events and IEventBus
├── tests/
│   └── PayFlow.Unit.Tests/     # xUnit tests
├── docs/bug-analysis/          # Bug reproduction guides
└── docker-compose.yml          # Single-container setup via PayFlow.Host
```
