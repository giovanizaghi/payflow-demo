using PayFlow.PointsApi.Domain;
using PayFlow.PointsApi.Repositories;
using PayFlow.Shared.Events;
using PayFlow.Shared.Messaging;

namespace PayFlow.PointsApi.Services;

public record EarnResult(bool Success, int PointsAwarded, int NewBalance, string? Error = null);
public record RedeemResult(bool Success, int NewBalance, string? Error = null);

public interface IPointsService
{
    Task<EarnResult> EarnForOrderAsync(Guid customerId, Guid orderId, decimal orderTotal);
    Task<RedeemResult> RedeemAsync(Guid customerId, int pointsToRedeem, Guid orderId);
    Task ReverseForOrderAsync(Guid customerId, Guid orderId);
    Task<int> GetBalanceAsync(Guid customerId);
    Task<IReadOnlyList<PointsTransaction>> GetTransactionsAsync(Guid customerId);
}

public class PointsService : IPointsService
{
    private const decimal PointsPerReal = 1m;

    private readonly IPointsRepository _repository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PointsService> _logger;

    public PointsService(IPointsRepository repository, IEventBus eventBus, ILogger<PointsService> logger)
    {
        _repository = repository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<EarnResult> EarnForOrderAsync(Guid customerId, Guid orderId, decimal orderTotal)
    {
        if (await _repository.OrderAlreadyProcessedAsync(orderId))
        {
            _logger.LogWarning("Order {OrderId} already processed for customer {CustomerId}", orderId, customerId);
            var existingAccount = await _repository.GetAccountAsync(customerId);
            return new EarnResult(true, 0, existingAccount?.Balance ?? 0);
        }

        // BUG #1: No lock between read and write.
        // Concurrent requests read the same EarnedThisMonth and both pass the cap.
        // See docs/bug-analysis/bug-01-race-condition.md
        var account = await _repository.GetOrCreateAccountAsync(customerId);

        var pointsToAward = (int)Math.Floor(orderTotal * PointsPerReal);
        var awarded = account.Earn(pointsToAward);

        await _repository.SaveAccountAsync(account);
        await _repository.RecordTransactionAsync(new PointsTransaction
        {
            CustomerId = customerId,
            OrderId = orderId,
            Points = awarded,
            Type = "earn",
            Description = $"Points earned for order {orderId}"
        });

        await _eventBus.PublishAsync(new PointsEarnedEvent(
            TransactionId: Guid.NewGuid(),
            CustomerId: customerId,
            OrderId: orderId,
            PointsAwarded: awarded,
            NewBalance: account.Balance,
            EarnedAt: DateTimeOffset.UtcNow));

        return new EarnResult(true, awarded, account.Balance);
    }

    public async Task<RedeemResult> RedeemAsync(Guid customerId, int pointsToRedeem, Guid orderId)
    {
        var account = await _repository.GetOrCreateAccountAsync(customerId);

        if (!account.TryRedeem(pointsToRedeem))
            return new RedeemResult(false, account.Balance, "Insufficient points balance");

        await _repository.SaveAccountAsync(account);
        await _repository.RecordTransactionAsync(new PointsTransaction
        {
            CustomerId = customerId,
            OrderId = orderId,
            Points = -pointsToRedeem,
            Type = "redeem",
            Description = $"Points redeemed for order {orderId}"
        });

        await _eventBus.PublishAsync(new PointsDeductedEvent(
            TransactionId: Guid.NewGuid(),
            CustomerId: customerId,
            OrderId: orderId,
            PointsDeducted: pointsToRedeem,
            Reason: "redemption",
            NewBalance: account.Balance,
            DeductedAt: DateTimeOffset.UtcNow));

        return new RedeemResult(true, account.Balance);
    }

    public Task ReverseForOrderAsync(Guid customerId, Guid orderId) =>
        throw new NotImplementedException(
            "BUG #2: This handler is missing. See docs/bug-analysis/bug-02-cancellation-gap.md");

    public async Task<int> GetBalanceAsync(Guid customerId)
    {
        var account = await _repository.GetAccountAsync(customerId);
        return account?.Balance ?? 0;
    }

    public Task<IReadOnlyList<PointsTransaction>> GetTransactionsAsync(Guid customerId) =>
        _repository.GetTransactionsAsync(customerId);
}
