namespace Amuse.Modules.Billing.Contracts;

using Amuse.Domain.SharedKernel;

public sealed record CheckoutSessionRequest(
    Guid PurchaseId,
    Guid PaymentTransactionId,
    long AmountMinor,
    string Currency,
    string ProductName,
    string SuccessUrl,
    string CancelUrl,
    Guid AccountId);

public sealed record CheckoutSessionResult(
    string SessionId,
    string CheckoutUrl);

public interface ICheckoutProvider
{
    Task<Result<CheckoutSessionResult>> CreateSessionAsync(
        CheckoutSessionRequest request,
        CancellationToken cancellationToken);

    Task<Result<CompletedCheckoutPayment>> GetCompletedPaymentAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken);

    Task<Result<RefundChargeResult>> RefundChargeAsync(
        string chargeId,
        CancellationToken cancellationToken);
}

public sealed record RefundChargeResult(
    string RefundId,
    long RefundFeeMinor);

public sealed record CompletedCheckoutPayment(
    string CheckoutSessionId,
    string ProviderReference,
    string? PaymentMethodFingerprint,
    long PspFeeMinor,
    long GrossMinor,
    string Currency);
