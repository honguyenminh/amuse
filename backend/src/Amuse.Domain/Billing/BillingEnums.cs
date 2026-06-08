namespace Amuse.Domain.Billing;

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Free = 2,
    Refunded = 3,
    PartiallyRefunded = 4,
    ChargedBack = 5,
}

public enum EntitlementStatus
{
    Active = 0,
    Revoked = 1,
}

public enum PurchasedUnit
{
    Track = 0,
    Release = 1,
}

public enum PayoutVerificationStatus
{
    NotStarted = 0,
    Submitted = 1,
    UnderReview = 2,
    Verified = 3,
    Rejected = 4,
}

public enum PayoutRail
{
    StripeGlobal = 0,
    ManualBank = 1,
}

public enum LegalEntityType
{
    Individual = 0,
    Company = 1,
}

public enum WithdrawalStatus
{
    Requested = 0,
    PendingApproval = 1,
    Approved = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
}

public enum JournalType
{
    Purchase = 0,
    Refund = 1,
    Chargeback = 2,
    HoldRelease = 3,
    Withdrawal = 4,
    Adjustment = 5,
    StreamSettlement = 6,
}

public enum EntryDirection
{
    Debit = 0,
    Credit = 1,
}

public enum ReferenceType
{
    Purchase = 0,
    StreamSettlement = 1,
    Adjustment = 2,
    Withdrawal = 3,
    Refund = 4,
    Chargeback = 5,
}

public enum RefundFeeBearer
{
    Platform = 0,
    Seller = 1,
}

public enum RefundInitiatorRole
{
    Platform = 0,
    Seller = 1,
}

public enum FxRateSource
{
    EcbDaily = 0,
    OpsManual = 1,
    StripeQuote = 2,
}

public enum LedgerAccountType
{
    PlatformCash = 0,
    VatPayable = 1,
    SellerPayablePending = 2,
    SellerPayableAvailable = 3,
    SellerPayableInPayout = 4,
    PlatformRevenue = 5,
    PspFeeExpense = 6,
    RefundLiability = 7,
}
