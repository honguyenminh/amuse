using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Billing;

public sealed class LedgerJournal
{
    public LedgerJournalId Id { get; private set; }
    public JournalType JournalType { get; private set; }
    public ReferenceType ReferenceType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateTimeOffset PostedAt { get; private set; }
    public DateTimeOffset? AvailableAt { get; private set; }
    private readonly List<LedgerEntry> _entries = [];

    public IReadOnlyList<LedgerEntry> Entries => _entries;

    private LedgerJournal()
    {
    }

    public static Result<LedgerJournal> Create(
        JournalType journalType,
        ReferenceType referenceType,
        Guid referenceId,
        string currency,
        DateTimeOffset postedAt,
        DateTimeOffset? availableAt,
        IEnumerable<LedgerEntry> entries)
    {
        var entryList = entries.ToList();
        if (entryList.Count == 0)
            return Result<LedgerJournal>.Failure(BillingErrors.InvalidLedgerJournal);

        var debitTotal = entryList.Where(e => e.Direction == EntryDirection.Debit).Sum(e => e.AmountMinor);
        var creditTotal = entryList.Where(e => e.Direction == EntryDirection.Credit).Sum(e => e.AmountMinor);
        if (debitTotal != creditTotal)
            return Result<LedgerJournal>.Failure(BillingErrors.InvalidLedgerJournal);

        var journal = new LedgerJournal
        {
            Id = LedgerJournalId.New(),
            JournalType = journalType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Currency = currency,
            PostedAt = postedAt,
            AvailableAt = availableAt,
        };

        foreach (var entry in entryList)
            journal._entries.Add(entry);

        return Result<LedgerJournal>.Success(journal);
    }
}

public sealed class LedgerEntry
{
    public LedgerEntryId Id { get; private set; }
    public LedgerJournalId JournalId { get; private set; }
    public LedgerAccountType AccountType { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public EntryDirection Direction { get; private set; }
    public long AmountMinor { get; private set; }
    public string Currency { get; private set; } = null!;

    private LedgerEntry()
    {
    }

    public static LedgerEntry Create(
        LedgerJournalId journalId,
        LedgerAccountType accountType,
        Guid? organizationId,
        EntryDirection direction,
        long amountMinor,
        string currency)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        return new LedgerEntry
        {
            Id = LedgerEntryId.New(),
            JournalId = journalId,
            AccountType = accountType,
            OrganizationId = organizationId,
            Direction = direction,
            AmountMinor = amountMinor,
            Currency = currency,
        };
    }
}
