using PayFlow.PointsApi.Domain;

namespace PayFlow.PointsApi.Repositories;

/// <remarks>
/// Uses non-thread-safe Dictionary intentionally to expose Bug #1.
/// Replace with EF Core + SQL Server and add RowVersion to PointsAccount
/// for optimistic concurrency in production.
/// </remarks>
public class InMemoryPointsRepository : IPointsRepository
{
    private readonly Dictionary<Guid, PointsAccount> _accounts = new();
    private readonly List<PointsTransaction> _transactions = new();
    private readonly HashSet<Guid> _processedOrders = new();
    private readonly HashSet<Guid> _reversedOrders = new();

    public Task<PointsAccount?> GetAccountAsync(Guid customerId)
    {
        _accounts.TryGetValue(customerId, out var account);
        return Task.FromResult(account);
    }

    public Task<PointsAccount> GetOrCreateAccountAsync(Guid customerId)
    {
        if (!_accounts.TryGetValue(customerId, out var account))
        {
            account = PointsAccount.Create(customerId);
            _accounts[customerId] = account;
        }
        return Task.FromResult(account);
    }

    public Task SaveAccountAsync(PointsAccount account)
    {
        _accounts[account.CustomerId] = account;
        return Task.CompletedTask;
    }

    public Task RecordTransactionAsync(PointsTransaction transaction)
    {
        _transactions.Add(transaction);
        if (transaction.Type == "earn" && transaction.OrderId.HasValue)
            _processedOrders.Add(transaction.OrderId.Value);
        if (transaction.Type == "reversal" && transaction.OrderId.HasValue)
            _reversedOrders.Add(transaction.OrderId.Value);
        return Task.CompletedTask;
    }

    public Task<bool> OrderAlreadyProcessedAsync(Guid orderId) =>
        Task.FromResult(_processedOrders.Contains(orderId));

    public Task<PointsTransaction?> GetEarnTransactionForOrderAsync(Guid orderId)
    {
        var tx = _transactions.FirstOrDefault(t => t.OrderId == orderId && t.Type == "earn");
        return Task.FromResult(tx);
    }

    public Task<bool> ReversalExistsForOrderAsync(Guid orderId) =>
        Task.FromResult(_reversedOrders.Contains(orderId));

    public Task<IReadOnlyList<PointsTransaction>> GetTransactionsAsync(Guid customerId)
    {
        var result = _transactions
            .Where(t => t.CustomerId == customerId)
            .ToList();
        return Task.FromResult<IReadOnlyList<PointsTransaction>>(result);
    }
}
