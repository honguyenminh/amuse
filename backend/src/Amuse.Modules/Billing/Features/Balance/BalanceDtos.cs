namespace Amuse.Modules.Billing.Features.Balance;

public sealed record CurrencyBalanceRow(
    string Currency,
    long PendingMinor,
    long AvailableMinor,
    long InPayoutMinor,
    long ReceivableMinor,
    long? UsdEquivalentMinor);

public sealed record OrgBalanceResponse(
    IReadOnlyList<CurrencyBalanceRow> Balances,
    bool GateBVerified,
    bool BlocksWithdrawals,
    DateTimeOffset? CooldownEndsAt,
    bool HasOutstandingReceivable);
