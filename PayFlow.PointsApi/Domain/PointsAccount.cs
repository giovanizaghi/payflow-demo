namespace PayFlow.PointsApi.Domain;

public class PointsAccount
{
    public Guid CustomerId { get; private set; }
    public int Balance { get; private set; }
    public int EarnedThisMonth { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }

    public const int MonthlyEarningCap = 500;

    private PointsAccount() { }

    public static PointsAccount Create(Guid customerId) => new()
    {
        CustomerId = customerId,
        Balance = 0,
        EarnedThisMonth = 0,
        LastUpdated = DateTimeOffset.UtcNow
    };

    /// <remarks>
    /// WARNING: Not thread-safe. Concurrent callers can read the same
    /// EarnedThisMonth value and both pass the monthly cap check.
    /// This is intentional for demo purposes. See bug-01-race-condition.md.
    /// </remarks>
    public int Earn(int points)
    {
        ResetMonthlyCounterIfNeeded();
        var headroom = MonthlyEarningCap - EarnedThisMonth;
        var awarded = Math.Min(points, headroom);
        Balance += awarded;
        EarnedThisMonth += awarded;
        LastUpdated = DateTimeOffset.UtcNow;
        return awarded;
    }

    public bool TryRedeem(int points)
    {
        if (Balance < points)
            return false;
        Balance -= points;
        LastUpdated = DateTimeOffset.UtcNow;
        return true;
    }

    private void ResetMonthlyCounterIfNeeded()
    {
        var now = DateTimeOffset.UtcNow;
        if (now.Year != LastUpdated.Year || now.Month != LastUpdated.Month)
            EarnedThisMonth = 0;
    }
}
