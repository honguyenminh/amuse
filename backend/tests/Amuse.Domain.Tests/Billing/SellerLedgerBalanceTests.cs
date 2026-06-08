using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Tests.Billing;

public sealed class SellerLedgerBalanceTests
{
    private static readonly OrganizationId OrgId = OrganizationId.New();

    [Fact]
    public void Aggregate_sums_pending_available_and_in_payout_per_currency()
    {
        var journalId = LedgerJournalId.New();
        var entries = new[]
        {
            CreateEntry(journalId, LedgerAccountType.SellerPayablePending, 3_000, EntryDirection.Credit),
            CreateEntry(journalId, LedgerAccountType.SellerPayableAvailable, 2_000, EntryDirection.Credit),
            CreateEntry(journalId, LedgerAccountType.SellerPayableAvailable, 500, EntryDirection.Debit),
            CreateEntry(journalId, LedgerAccountType.SellerPayableInPayout, 1_000, EntryDirection.Credit),
        };

        var balances = SellerLedgerBalance.Aggregate(
            entries,
            OrgId.Value,
            new Dictionary<string, long> { ["USD"] = 250 });

        var usd = Assert.Single(balances);
        Assert.Equal("USD", usd.Currency);
        Assert.Equal(3_000, usd.PendingMinor);
        Assert.Equal(1_500, usd.AvailableMinor);
        Assert.Equal(1_000, usd.InPayoutMinor);
        Assert.Equal(250, usd.ReceivableMinor);
    }

    [Fact]
    public void PostWithdrawalReserve_moves_available_to_in_payout()
    {
        var withdrawalId = WithdrawalRequestId.New();
        var money = Money.Create(1_200, "USD").Value!;
        var journal = JournalPoster.PostWithdrawalReserve(withdrawalId, OrgId, money, DateTimeOffset.UtcNow);

        Assert.True(journal.IsSuccess);
        var availableDebit = journal.Value!.Entries.Single(entry =>
            entry.AccountType == LedgerAccountType.SellerPayableAvailable
            && entry.Direction == EntryDirection.Debit);
        var inPayoutCredit = journal.Value!.Entries.Single(entry =>
            entry.AccountType == LedgerAccountType.SellerPayableInPayout
            && entry.Direction == EntryDirection.Credit);

        Assert.Equal(1_200, availableDebit.AmountMinor);
        Assert.Equal(1_200, inPayoutCredit.AmountMinor);
    }

    private static LedgerEntry CreateEntry(
        LedgerJournalId journalId,
        LedgerAccountType accountType,
        long amountMinor,
        EntryDirection direction) =>
        LedgerEntry.Create(
            journalId,
            accountType,
            OrgId.Value,
            direction,
            amountMinor,
            "USD");
}
