using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Catalog;

public sealed class RoyaltySplit
{
    public const int TotalShareBps = 10_000;

    public RoyaltySplitId Id { get; private set; }
    public TrackId TrackId { get; private set; }
    public OrganizationId PayeeOrganizationId { get; private set; }
    public int ShareBps { get; private set; }
    public OrganizationId ListingOrganizationId { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }

    private RoyaltySplit()
    {
    }

    private RoyaltySplit(
        RoyaltySplitId id,
        TrackId trackId,
        OrganizationId payeeOrganizationId,
        int shareBps,
        OrganizationId listingOrganizationId,
        DateTimeOffset effectiveFrom)
    {
        Id = id;
        TrackId = trackId;
        PayeeOrganizationId = payeeOrganizationId;
        ShareBps = shareBps;
        ListingOrganizationId = listingOrganizationId;
        EffectiveFrom = effectiveFrom;
    }

    public static Result<RoyaltySplit> Create(
        RoyaltySplitId id,
        TrackId trackId,
        OrganizationId payeeOrganizationId,
        int shareBps,
        OrganizationId listingOrganizationId,
        DateTimeOffset effectiveFrom)
    {
        if (effectiveFrom.Offset != TimeSpan.Zero)
            return Result<RoyaltySplit>.Failure(CatalogErrors.InvalidRoyaltySplit);

        if (shareBps <= 0 || shareBps > TotalShareBps)
            return Result<RoyaltySplit>.Failure(CatalogErrors.InvalidRoyaltySplit);

        return Result<RoyaltySplit>.Success(
            new RoyaltySplit(
                id,
                trackId,
                payeeOrganizationId,
                shareBps,
                listingOrganizationId,
                effectiveFrom));
    }

    public static Result<IReadOnlyList<RoyaltySplit>> ReplaceForTrack(
        TrackId trackId,
        OrganizationId listingOrganizationId,
        IReadOnlyList<RoyaltySplitEntry> entries,
        DateTimeOffset effectiveFrom)
    {
        if (effectiveFrom.Offset != TimeSpan.Zero)
            return Result<IReadOnlyList<RoyaltySplit>>.Failure(CatalogErrors.InvalidRoyaltySplit);

        if (entries.Count == 0)
            return Result<IReadOnlyList<RoyaltySplit>>.Success([]);

        var validation = ValidateEntries(entries);
        if (!validation.IsSuccess)
            return Result<IReadOnlyList<RoyaltySplit>>.Failure(validation.Error!);

        var splits = new List<RoyaltySplit>(entries.Count);
        foreach (var entry in entries)
        {
            var createResult = Create(
                RoyaltySplitId.New(),
                trackId,
                entry.PayeeOrganizationId,
                entry.ShareBps,
                listingOrganizationId,
                effectiveFrom);

            if (!createResult.IsSuccess)
                return Result<IReadOnlyList<RoyaltySplit>>.Failure(createResult.Error!);

            splits.Add(createResult.Value!);
        }

        return Result<IReadOnlyList<RoyaltySplit>>.Success(splits);
    }

    public static Result ValidateEntries(IReadOnlyList<RoyaltySplitEntry> entries)
    {
        if (entries.Count == 0)
            return Result.Success();

        var total = 0;
        var payees = new HashSet<Guid>();

        foreach (var entry in entries)
        {
            if (entry.ShareBps <= 0 || entry.ShareBps > TotalShareBps)
                return Result.Failure(CatalogErrors.InvalidRoyaltySplit);

            if (!payees.Add(entry.PayeeOrganizationId.Value))
                return Result.Failure(CatalogErrors.DuplicateRoyaltyPayee);

            total += entry.ShareBps;
        }

        if (total != TotalShareBps)
            return Result.Failure(CatalogErrors.RoyaltySplitSumInvalid);

        return Result.Success();
    }

    public static Result ValidateForPublish(
        TrackId trackId,
        IReadOnlyList<RoyaltySplit> persistedSplits)
    {
        if (persistedSplits.Count == 0)
            return Result.Success();

        var entries = persistedSplits
            .Where(split => split.TrackId == trackId)
            .Select(split => new RoyaltySplitEntry(split.PayeeOrganizationId, split.ShareBps))
            .ToArray();

        return ValidateEntries(entries);
    }
}

public readonly record struct RoyaltySplitEntry(
    OrganizationId PayeeOrganizationId,
    int ShareBps);
