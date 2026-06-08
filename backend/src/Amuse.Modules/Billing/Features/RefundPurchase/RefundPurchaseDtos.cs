namespace Amuse.Modules.Billing.Features.RefundPurchase;

public sealed record RefundPurchaseRequest(
    string Reason,
    string? RefundFeeBearer);

public sealed record RefundPurchaseResponse(
    Guid PurchaseId,
    string PaymentStatus,
    DateTimeOffset RefundedAt);
