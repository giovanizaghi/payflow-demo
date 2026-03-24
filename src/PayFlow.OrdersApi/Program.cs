using Microsoft.OpenApi;
using PayFlow.OrdersApi.Repositories;
using PayFlow.OrdersApi.Services;
using PayFlow.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PayFlow Orders API",
        Version = "v1",
        Description = "Order management service for the PayFlow e-commerce platform"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PayFlow Orders API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "PayFlow.OrdersApi",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();
