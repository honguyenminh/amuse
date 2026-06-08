using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public sealed record AllocationPayeeLine(
    Guid TrackId,
    OrganizationId PayeeOrganizationId,
    int ShareBps,
    long AmountMinor);

public static class PurchaseAllocation
{
    public static Result<IReadOnlyList<AllocationPayeeLine>> AllocateTrack(
        Guid trackId,
        OrganizationId listingOrganizationId,
        long netToSellersMinor,
        IReadOnlyList<RoyaltySplitSnapshot> splits)
    {
        if (netToSellersMinor < 0)
            return Result<IReadOnlyList<AllocationPayeeLine>>.Failure(BillingErrors.InvalidLedgerJournal);

        var effectiveSplits = ResolveTrackSplits(trackId, listingOrganizationId, splits);
        return DistributeByShares(trackId, netToSellersMinor, effectiveSplits);
    }

    public static Result<IReadOnlyList<AllocationPayeeLine>> AllocateRelease(
        long netToSellersMinor,
        IReadOnlyList<ReleaseTrackAllocationInput> tracks)
    {
        if (netToSellersMinor < 0)
            return Result<IReadOnlyList<AllocationPayeeLine>>.Failure(BillingErrors.InvalidLedgerJournal);

        if (tracks.Count == 0)
            return Result<IReadOnlyList<AllocationPayeeLine>>.Failure(BillingErrors.InvalidLedgerJournal);

        var trackWeights = ComputeTrackWeights(tracks);
        var trackNets = DistributeAmount(netToSellersMinor, trackWeights);

        var lines = new List<AllocationPayeeLine>();
        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            var trackNet = trackNets[i];
            if (trackNet == 0)
                continue;

            var effectiveSplits = ResolveTrackSplits(track.TrackId, track.ListingOrganizationId, track.Splits);
            var splitResult = DistributeByShares(track.TrackId, trackNet, effectiveSplits);
            if (!splitResult.IsSuccess)
                return splitResult;

            lines.AddRange(splitResult.Value!);
        }

        return Result<IReadOnlyList<AllocationPayeeLine>>.Success(lines);
    }

    private static IReadOnlyList<RoyaltySplitSnapshot> ResolveTrackSplits(
        Guid trackId,
        OrganizationId listingOrganizationId,
        IReadOnlyList<RoyaltySplitSnapshot> splits)
    {
        var trackSplits = splits.Where(s => s.TrackId == trackId).ToArray();
        if (trackSplits.Length > 0)
            return trackSplits;

        return
        [
            new RoyaltySplitSnapshot(trackId, listingOrganizationId, 10_000),
        ];
    }

    private static long[] ComputeTrackWeights(IReadOnlyList<ReleaseTrackAllocationInput> tracks)
    {
        var sumFloors = tracks.Sum(t => t.PriceFloorMinor);
        if (sumFloors > 0)
            return tracks.Select(t => t.PriceFloorMinor).ToArray();

        return Enumerable.Repeat(1L, tracks.Count).ToArray();
    }

    private static Result<IReadOnlyList<AllocationPayeeLine>> DistributeByShares(
        Guid trackId,
        long amountMinor,
        IReadOnlyList<RoyaltySplitSnapshot> splits)
    {
        if (amountMinor == 0)
            return Result<IReadOnlyList<AllocationPayeeLine>>.Success([]);

        var weights = splits.Select(s => (long)s.ShareBps).ToArray();
        var amounts = DistributeAmount(amountMinor, weights);

        var lines = new List<AllocationPayeeLine>(splits.Count);
        for (var i = 0; i < splits.Count; i++)
        {
            if (amounts[i] == 0)
                continue;

            lines.Add(new AllocationPayeeLine(
                trackId,
                splits[i].PayeeOrganizationId,
                splits[i].ShareBps,
                amounts[i]));
        }

        return Result<IReadOnlyList<AllocationPayeeLine>>.Success(lines);
    }

    /// <summary>
    /// Largest-remainder allocation in minor units; last payee absorbs any residual.
    /// </summary>
    internal static long[] DistributeAmount(long totalMinor, IReadOnlyList<long> weights)
    {
        if (weights.Count == 0)
            return [];

        if (totalMinor == 0)
            return new long[weights.Count];

        var weightSum = weights.Sum();
        if (weightSum == 0)
        {
            var equal = new long[weights.Count];
            var baseShare = totalMinor / weights.Count;
            var remainder = totalMinor - baseShare * weights.Count;
            for (var i = 0; i < equal.Length; i++)
                equal[i] = baseShare + (i < remainder ? 1 : 0);

            return equal;
        }

        var raw = weights
            .Select(w => (double)totalMinor * w / weightSum)
            .ToArray();

        var floors = raw.Select(v => (long)Math.Floor(v)).ToArray();
        var allocated = floors.Sum();
        var residual = totalMinor - allocated;

        var fractions = raw
            .Select((value, index) => (Index: index, Fraction: value - floors[index]))
            .OrderByDescending(x => x.Fraction)
            .ThenBy(x => x.Index)
            .ToArray();

        for (var i = 0; i < residual && i < fractions.Length; i++)
            floors[fractions[i].Index]++;

        return floors;
    }
}

public readonly record struct RoyaltySplitSnapshot(
    Guid TrackId,
    OrganizationId PayeeOrganizationId,
    int ShareBps);

public sealed record ReleaseTrackAllocationInput(
    Guid TrackId,
    OrganizationId ListingOrganizationId,
    long PriceFloorMinor,
    IReadOnlyList<RoyaltySplitSnapshot> Splits);
