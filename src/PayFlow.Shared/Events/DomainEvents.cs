namespace PayFlow.Shared.Events;

public record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal OrderTotal,
    string CurrencyCode,
    DateTimeOffset PlacedAt);

public record OrderCancelledEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal OrderTotal,
    DateTimeOffset CancelledAt);

public record PointsEarnedEvent(
    Guid TransactionId,
    Guid CustomerId,
    Guid OrderId,
    int PointsAwarded,
    int NewBalance,
    DateTimeOffset EarnedAt);

public record PointsDeductedEvent(
    Guid TransactionId,
    Guid CustomerId,
    Guid? OrderId,
    int PointsDeducted,
    string Reason,
    int NewBalance,
    DateTimeOffset DeductedAt);

public record PointsExpiringEvent(
    Guid CustomerId,
    int PointsExpiring,
    DateTimeOffset ExpiresAt);

public record PaymentAttemptedEvent(
    Guid PaymentId,
    Guid CustomerId,
    Guid OrderId,
    decimal Amount,
    string Method,
    bool Success,
    string? FailureReason,
    DateTimeOffset AttemptedAt);
