using PayFlow.PointsApi.Domain;

namespace PayFlow.PointsApi.Repositories;

public interface IPointsRepository
{
    Task<PointsAccount?> GetAccountAsync(Guid customerId);
    Task<PointsAccount> GetOrCreateAccountAsync(Guid customerId);
    Task SaveAccountAsync(PointsAccount account);
    Task RecordTransactionAsync(PointsTransaction transaction);
    Task<bool> OrderAlreadyProcessedAsync(Guid orderId);
    Task<PointsTransaction?> GetEarnTransactionForOrderAsync(Guid orderId);
    Task<bool> ReversalExistsForOrderAsync(Guid orderId);
    Task<IReadOnlyList<PointsTransaction>> GetTransactionsAsync(Guid customerId);
}
