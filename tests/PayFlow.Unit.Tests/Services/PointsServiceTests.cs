using Microsoft.Extensions.Logging.Abstractions;
using PayFlow.PointsApi.Domain;
using PayFlow.PointsApi.Repositories;
using PayFlow.PointsApi.Services;
using PayFlow.Shared.Messaging;

namespace PayFlow.Unit.Tests.Services;

public class PointsServiceTests
{
    private static PointsService CreateService(out InMemoryPointsRepository repo)
    {
        repo = new InMemoryPointsRepository();
        var bus = new InMemoryEventBus();
        var logger = NullLogger<PointsService>.Instance;
        return new PointsService(repo, bus, logger);
    }

    [Fact]
    public async Task EarnForOrder_SingleRequest_AwardsCorrectPoints()
    {
        var svc = CreateService(out _);
        var customerId = Guid.NewGuid();

        var result = await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 100m);

        Assert.True(result.Success);
        Assert.Equal(100, result.PointsAwarded);
        Assert.Equal(100, result.NewBalance);
    }

    [Fact]
    public async Task EarnForOrder_RespectsMonthlyCapSingleRequest()
    {
        var svc = CreateService(out _);
        var customerId = Guid.NewGuid();

        await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 480m);
        var result = await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 100m);

        Assert.Equal(20, result.PointsAwarded);
        Assert.Equal(500, result.NewBalance);
    }

    [Fact]
    public async Task EarnForOrder_DuplicateOrderId_IsIdempotent()
    {
        var svc = CreateService(out _);
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await svc.EarnForOrderAsync(customerId, orderId, 100m);
        await svc.EarnForOrderAsync(customerId, orderId, 100m);

        var balance = await svc.GetBalanceAsync(customerId);
        Assert.Equal(100, balance);
    }

    [Fact]
    public async Task EarnForOrder_ZeroTotal_AwardsZeroPoints()
    {
        var svc = CreateService(out _);
        var customerId = Guid.NewGuid();

        var result = await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 0m);

        Assert.True(result.Success);
        Assert.Equal(0, result.PointsAwarded);
    }

    [Fact]
    public async Task Redeem_WithSufficientBalance_Succeeds()
    {
        var svc = CreateService(out _);
        var customerId = Guid.NewGuid();

        await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 200m);
        var result = await svc.RedeemAsync(customerId, 150, Guid.NewGuid());

        Assert.True(result.Success);
        Assert.Equal(50, result.NewBalance);
    }

    [Fact]
    public async Task Redeem_WithInsufficientBalance_Fails()
    {
        var svc = CreateService(out _);
        var customerId = Guid.NewGuid();

        await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 50m);
        var result = await svc.RedeemAsync(customerId, 100, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Insufficient points balance", result.Error);
        Assert.Equal(50, result.NewBalance);
    }

    [Fact]
    public async Task Redeem_ExactBalance_Succeeds()
    {
        var svc = CreateService(out _);
        var customerId = Guid.NewGuid();

        await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 100m);
        var result = await svc.RedeemAsync(customerId, 100, Guid.NewGuid());

        Assert.True(result.Success);
        Assert.Equal(0, result.NewBalance);
    }

    [Fact]
    public async Task GetBalance_NonExistentCustomer_ReturnsZero()
    {
        var svc = CreateService(out _);

        var balance = await svc.GetBalanceAsync(Guid.NewGuid());

        Assert.Equal(0, balance);
    }

    [Fact]
    public async Task GetTransactions_AfterEarnAndRedeem_ReturnsBothEntries()
    {
        var svc = CreateService(out _);
        var customerId = Guid.NewGuid();

        await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 200m);
        await svc.RedeemAsync(customerId, 50, Guid.NewGuid());

        var transactions = await svc.GetTransactionsAsync(customerId);
        Assert.Equal(2, transactions.Count);
    }

    [Fact]
    public async Task ReverseForOrder_ThrowsNotImplementedException()
    {
        var svc = CreateService(out _);

        await Assert.ThrowsAsync<NotImplementedException>(
            () => svc.ReverseForOrderAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    /// <summary>
    /// This test intentionally FAILS to expose Bug #1 (race condition).
    /// Both concurrent requests read EarnedThisMonth=480 before either writes back.
    /// Both pass the cap check and award 20 points each — balance becomes 520.
    /// Expected: 500. Actual: 520. Fix with optimistic concurrency.
    /// See docs/bug-analysis/bug-01-race-condition.md
    /// </summary>
    [Fact]
    public async Task EarnForOrder_ConcurrentRequests_ShouldRespectMonthlyCap_ButDoesNot()
    {
        var svc = CreateService(out var repo);
        var customerId = Guid.NewGuid();

        // Stable baseline: earn 480 sequentially
        await svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 480m);

        // Retrieve the account and inject a test hook that pauses inside Earn()
        // after computing `awarded` but before writing Balance/EarnedThisMonth.
        // This widens the race window to 50ms, allowing a second thread to
        // read EarnedThisMonth=480 and compute awarded=20 concurrently.
        var account = await repo.GetOrCreateAccountAsync(customerId);
        var gate = new ManualResetEventSlim(false);
        account._onAfterAwardedComputed = () =>
        {
            // Thread 1 signals it has computed awarded=20, then waits.
            // Thread 2 will proceed through the same path before Thread 1 writes back.
            gate.Set();
            Thread.Sleep(50);
        };

        // Fire two concurrent requests simultaneously
        var task1 = Task.Run(() => svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 20m).GetAwaiter().GetResult());
        // Wait until task1 is inside Earn() past the gap point, then start task2
        gate.Wait();
        account._onAfterAwardedComputed = null; // task2 runs without the hook
        var task2 = svc.EarnForOrderAsync(customerId, Guid.NewGuid(), 20m);

        await Task.WhenAll(task1, task2);

        var finalBalance = await svc.GetBalanceAsync(customerId);

        // BUG #1: task1 computed awarded=20 (EarnedThisMonth was 480) but paused.
        // task2 also saw EarnedThisMonth=480, computed awarded=20, and completed first.
        // task1 then resumed and wrote Balance += 20 on top — final balance = 520.
        // This assertion FAILS intentionally. Expected behavior: 500.
        Assert.Equal(500, finalBalance);
    }
}

