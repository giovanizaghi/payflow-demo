using Microsoft.OpenApi;
using PayFlow.PointsApi.Repositories;
using PayFlow.PointsApi.Services;
using PayFlow.Shared.Events;
using PayFlow.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IPointsRepository, InMemoryPointsRepository>();
builder.Services.AddScoped<IPointsService, PointsService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PayFlow Points API",
        Version = "v1",
        Description = "Loyalty points engine for the PayFlow e-commerce platform"
    });
});

var app = builder.Build();

var eventBus = app.Services.GetRequiredService<IEventBus>();
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

// OrderCancelledEvent → reverse points
eventBus.Subscribe<OrderCancelledEvent>(async e =>
{
    using var scope = scopeFactory.CreateScope();
    var pointsService = scope.ServiceProvider.GetRequiredService<IPointsService>();
    await pointsService.ReverseForOrderAsync(e.CustomerId, e.OrderId);
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PayFlow Points API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
