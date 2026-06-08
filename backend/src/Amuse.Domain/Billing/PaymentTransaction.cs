using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Billing;

public sealed class PaymentTransaction
{
    public PaymentTransactionId Id { get; private set; }
    public PurchaseId PurchaseId { get; private set; }
    public AccountId AccountId { get; private set; }
    public long GrossMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public string? ProviderReference { get; private set; }
    public string? CheckoutSessionId { get; private set; }
    public string? PaymentMethodFingerprint { get; private set; }
    public long? PspFeeMinor { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private PaymentTransaction()
    {
    }

    public static Result<PaymentTransaction> CreatePending(
        PurchaseId purchaseId,
        AccountId accountId,
        Money gross,
        DateTimeOffset now)
    {
        if (gross.IsZero)
            return Result<PaymentTransaction>.Failure(BillingErrors.InvalidPaymentStatusTransition);

        return Result<PaymentTransaction>.Success(new PaymentTransaction
        {
            Id = PaymentTransactionId.New(),
            PurchaseId = purchaseId,
            AccountId = accountId,
            GrossMinor = gross.AmountMinor,
            Currency = gross.Currency,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
        });
    }

    public Result AssignCheckoutSession(string checkoutSessionId)
    {
        if (Status is not PaymentStatus.Pending)
            return Result.Failure(BillingErrors.InvalidPaymentStatusTransition);

        if (string.IsNullOrWhiteSpace(checkoutSessionId))
            return Result.Failure(BillingErrors.CheckoutSessionNotFound);

        CheckoutSessionId = checkoutSessionId.Trim();
        return Result.Success();
    }

    public Result MarkCompleted(
        string providerReference,
        string? paymentMethodFingerprint,
        long pspFeeMinor,
        DateTimeOffset now)
    {
        if (Status is not PaymentStatus.Pending)
            return Result.Failure(BillingErrors.InvalidPaymentStatusTransition);

        if (pspFeeMinor < 0)
            return Result.Failure(BillingErrors.InvalidPaymentStatusTransition);

        Status = PaymentStatus.Paid;
        ProviderReference = providerReference;
        PaymentMethodFingerprint = paymentMethodFingerprint;
        PspFeeMinor = pspFeeMinor;
        CompletedAt = now;
        return Result.Success();
    }

    public Result MarkRefunded(DateTimeOffset now)
    {
        if (Status is not PaymentStatus.Paid)
            return Result.Failure(BillingErrors.InvalidPaymentStatusTransition);

        Status = PaymentStatus.Refunded;
        CompletedAt = now;
        return Result.Success();
    }

    public Result MarkChargedBack(DateTimeOffset now)
    {
        if (Status is not PaymentStatus.Paid)
            return Result.Failure(BillingErrors.InvalidPaymentStatusTransition);

        Status = PaymentStatus.ChargedBack;
        CompletedAt = now;
        return Result.Success();
    }
}
