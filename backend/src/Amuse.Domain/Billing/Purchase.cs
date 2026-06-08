using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public sealed class Purchase
{
    public PurchaseId Id { get; private set; }
    public AccountId AccountId { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public PurchasedUnit PurchasedUnit { get; private set; }
    public Guid? TrackId { get; private set; }
    public Guid? ReleaseId { get; private set; }
    public long PriceSnapshotMinor { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentStatus PaymentStatus { get; private set; }
    public EntitlementStatus EntitlementStatus { get; private set; }
    public DateTimeOffset PurchasedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public PaymentTransactionId? PaymentTransactionId { get; private set; }
    public RefundInitiatorRole? RefundInitiatorRole { get; private set; }
    public AccountId? RefundInitiatedByAccountId { get; private set; }
    public string? RefundReason { get; private set; }
    public RefundFeeBearer? RefundFeeBearer { get; private set; }
    public DateTimeOffset? RefundRequestedAt { get; private set; }
    public DateTimeOffset? RefundedAt { get; private set; }

    private Purchase()
    {
    }

    public static Result<Purchase> AcquireFreeTrack(
        AccountId accountId,
        OrganizationId organizationId,
        Guid trackId,
        Money priceSnapshot,
        DateTimeOffset now)
    {
        if (!priceSnapshot.IsZero)
            return Result<Purchase>.Failure(BillingErrors.NotFreeEligible);

        return CreateCore(
            accountId,
            organizationId,
            PurchasedUnit.Track,
            trackId,
            null,
            priceSnapshot,
            PaymentStatus.Free,
            now);
    }

    public static Result<Purchase> AcquireFreeRelease(
        AccountId accountId,
        OrganizationId organizationId,
        Guid releaseId,
        Money priceSnapshot,
        DateTimeOffset now)
    {
        if (!priceSnapshot.IsZero)
            return Result<Purchase>.Failure(BillingErrors.NotFreeEligible);

        return CreateCore(
            accountId,
            organizationId,
            PurchasedUnit.Release,
            null,
            releaseId,
            priceSnapshot,
            PaymentStatus.Free,
            now);
    }

    public static Result<Purchase> CreatePaidTrack(
        AccountId accountId,
        OrganizationId organizationId,
        Guid trackId,
        Money amount,
        DateTimeOffset now)
    {
        if (amount.IsZero)
            return Result<Purchase>.Failure(BillingErrors.InvalidPaymentStatusTransition);

        return CreateCore(
            accountId,
            organizationId,
            PurchasedUnit.Track,
            trackId,
            null,
            amount,
            PaymentStatus.Pending,
            now);
    }

    public static Result<Purchase> CreatePaidRelease(
        AccountId accountId,
        OrganizationId organizationId,
        Guid releaseId,
        Money amount,
        DateTimeOffset now)
    {
        if (amount.IsZero)
            return Result<Purchase>.Failure(BillingErrors.InvalidPaymentStatusTransition);

        return CreateCore(
            accountId,
            organizationId,
            PurchasedUnit.Release,
            null,
            releaseId,
            amount,
            PaymentStatus.Pending,
            now);
    }

    public Result BeginRefund(
        AccountId initiatedBy,
        RefundInitiatorRole initiatorRole,
        string reason,
        RefundFeeBearer feeBearer,
        DateTimeOffset now)
    {
        if (PaymentStatus is not PaymentStatus.Paid)
            return Result.Failure(BillingErrors.RefundNotEligible);

        if (RefundRequestedAt.HasValue)
            return Result.Failure(BillingErrors.RefundInProgress);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(BillingErrors.RefundReasonRequired);

        RefundInitiatedByAccountId = initiatedBy;
        RefundInitiatorRole = initiatorRole;
        RefundReason = reason.Trim();
        RefundFeeBearer = feeBearer;
        RefundRequestedAt = now;
        return Result.Success();
    }

    public Result MarkRefunded(DateTimeOffset now)
    {
        if (PaymentStatus is PaymentStatus.Refunded)
            return Result.Failure(BillingErrors.RefundAlreadyProcessed);

        if (PaymentStatus is not PaymentStatus.Paid)
            return Result.Failure(BillingErrors.InvalidPaymentStatusTransition);

        PaymentStatus = PaymentStatus.Refunded;
        RefundedAt = now;
        return Result.Success();
    }

    public Result MarkChargedBack(DateTimeOffset now)
    {
        if (PaymentStatus is PaymentStatus.ChargedBack)
            return Result.Failure(BillingErrors.ChargebackAlreadyProcessed);

        if (PaymentStatus is not PaymentStatus.Paid)
            return Result.Failure(BillingErrors.ChargebackNotEligible);

        PaymentStatus = PaymentStatus.ChargedBack;
        RefundedAt = now;
        return Result.Success();
    }

    public Result MarkPaid(PaymentTransactionId paymentTransactionId, DateTimeOffset paidAt)
    {
        if (PaymentStatus is not PaymentStatus.Pending)
            return Result.Failure(BillingErrors.InvalidPaymentStatusTransition);

        PaymentStatus = PaymentStatus.Paid;
        PaymentTransactionId = paymentTransactionId;
        PaidAt = paidAt;
        return Result.Success();
    }

    public Result RevokeEntitlement()
    {
        if (EntitlementStatus == EntitlementStatus.Revoked)
            return Result.Failure(BillingErrors.InvalidEntitlementStatusTransition);

        EntitlementStatus = EntitlementStatus.Revoked;
        return Result.Success();
    }

    public bool HasActiveEntitlement => EntitlementStatus == EntitlementStatus.Active;

    private static Result<Purchase> CreateCore(
        AccountId accountId,
        OrganizationId organizationId,
        PurchasedUnit purchasedUnit,
        Guid? trackId,
        Guid? releaseId,
        Money amount,
        PaymentStatus paymentStatus,
        DateTimeOffset now)
    {
        if (purchasedUnit == PurchasedUnit.Track && trackId is null)
            return Result<Purchase>.Failure(BillingErrors.PurchaseNotFound);

        if (purchasedUnit == PurchasedUnit.Release && releaseId is null)
            return Result<Purchase>.Failure(BillingErrors.PurchaseNotFound);

        return Result<Purchase>.Success(new Purchase
        {
            Id = PurchaseId.New(),
            AccountId = accountId,
            OrganizationId = organizationId,
            PurchasedUnit = purchasedUnit,
            TrackId = trackId,
            ReleaseId = releaseId,
            PriceSnapshotMinor = amount.AmountMinor,
            Currency = amount.Currency,
            PaymentStatus = paymentStatus,
            EntitlementStatus = EntitlementStatus.Active,
            PurchasedAt = now,
            PaidAt = paymentStatus == PaymentStatus.Free ? now : null,
        });
    }
}
