using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OpenApi;
using PayFlow.OrdersApi.Controllers;
using PayFlow.OrdersApi.Repositories;
using PayFlow.OrdersApi.Services;
using PayFlow.PointsApi.Controllers;
using PayFlow.PointsApi.Repositories;
using PayFlow.PointsApi.Services;
using PayFlow.Shared.Events;
using PayFlow.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

// ── Shared infrastructure (one instance for the whole process) ────────────────
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IPointsRepository, InMemoryPointsRepository>();
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

// ── Domain services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<IPointsService, PointsService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ── Controllers from both assemblies ─────────────────────────────────────────
// Exclude PayFlow.PointsApi's HealthController — Host owns /health via minimal API
builder.Services.AddControllers()
    .ConfigureApplicationPartManager(apm =>
        apm.FeatureProviders.Add(new ExcludeControllerFeatureProvider("HealthController")))
    .AddApplicationPart(typeof(PointsController).Assembly)
    .AddApplicationPart(typeof(OrdersController).Assembly);

// ── Swagger — two separate documents ─────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("points", new OpenApiInfo
    {
        Title = "PayFlow Points API",
        Version = "v1",
        Description = "Loyalty points service"
    });
    options.SwaggerDoc("orders", new OpenApiInfo
    {
        Title = "PayFlow Orders API",
        Version = "v1",
        Description = "Order management service"
    });

    // Route each controller to its own doc by GroupName
    options.DocInclusionPredicate((docName, apiDesc) =>
        apiDesc.GroupName == docName);
});

var app = builder.Build();

// ── Swagger UI ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/points/swagger.json", "PayFlow Points API v1");
    options.SwaggerEndpoint("/swagger/orders/swagger.json", "PayFlow Orders API v1");
    options.RoutePrefix = "swagger";
});

// ── Event bus wiring ──────────────────────────────────────────────────────────
var eventBus = app.Services.GetRequiredService<IEventBus>();
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

// OrderPlacedEvent → earn points
eventBus.Subscribe<OrderPlacedEvent>(async e =>
{
    using var scope = scopeFactory.CreateScope();
    var pointsService = scope.ServiceProvider.GetRequiredService<IPointsService>();
    await pointsService.EarnForOrderAsync(e.CustomerId, e.OrderId, e.OrderTotal);
});

// ================================================================
// BUG #2: OrderCancelledEvent subscription is intentionally MISSING
// ================================================================
// OrdersApi publishes OrderCancelledEvent correctly (see OrderService.cs).
// PointsApi never handles it — points are never reversed on cancellation.
//
// TO REPRODUCE THE BUG:
//   1. POST /api/orders          → place order, points are earned automatically
//   2. GET  /api/points/{id}/balance → see points credited
//   3. DELETE /api/orders/{id}   → cancel order
//   4. GET  /api/points/{id}/balance → points still there ← BUG
//
// TO FIX (uncomment after showing the bug live):
//
// eventBus.Subscribe<OrderCancelledEvent>(async e =>
// {
//     using var scope = scopeFactory.CreateScope();
//     var pointsService = scope.ServiceProvider.GetRequiredService<IPointsService>();
//     await pointsService.ReverseForOrderAsync(e.CustomerId, e.OrderId);
// });
// ================================================================

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "PayFlow.Host",
    apis = new[] { "points", "orders" },
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();

/// <summary>
/// Removes a controller type from the discovered controller list before the MVC
/// pipeline processes it. Used to suppress HealthController from PayFlow.PointsApi
/// so that PayFlow.Host can own the /health endpoint via a minimal API route.
/// </summary>
class ExcludeControllerFeatureProvider(string controllerTypeName) : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var toRemove = feature.Controllers
            .Where(c => c.Name == controllerTypeName)
            .ToList();
        foreach (var c in toRemove)
            feature.Controllers.Remove(c);
    }
}
