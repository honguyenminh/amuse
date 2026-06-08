namespace Amuse.Modules.Discovery.Features.Search;

internal static class DiscoverySearchScoring
{
    internal const int ExactTitleScore = 1000;
    internal const int PrefixTitleScore = 800;
    internal const int SubstringTitleScore = 400;
    internal const int ExactSlugScore = 700;
    internal const int PrefixSlugScore = 500;
    internal const int SubstringSlugScore = 300;
    internal const int FullNerf = 200;
    internal const int ReducedNerf = 50;
    internal const int HighMatchThreshold = 800;

    internal sealed record SearchMatchCandidate(
        string Kind,
        string Title,
        IReadOnlyList<string> Slugs,
        bool IsVerified);

    internal static int ScoreTextMatch(string query, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        if (string.Equals(text, query, StringComparison.OrdinalIgnoreCase))
            return ExactTitleScore;

        if (text.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return PrefixTitleScore;

        if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
            return SubstringTitleScore;

        return 0;
    }

    internal static int ScoreSlugMatch(string query, string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return 0;

        if (string.Equals(slug, query, StringComparison.OrdinalIgnoreCase))
            return ExactSlugScore;

        if (slug.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return PrefixSlugScore;

        if (slug.Contains(query, StringComparison.OrdinalIgnoreCase))
            return SubstringSlugScore;

        return 0;
    }

    internal static int ComputeMatchScore(string query, SearchMatchCandidate candidate)
    {
        var titleScore = ScoreTextMatch(query, candidate.Title);
        var slugScore = candidate.Slugs.Count == 0
            ? 0
            : candidate.Slugs.Max(slug => ScoreSlugMatch(query, slug));

        return Math.Max(titleScore, slugScore);
    }

    internal static int ApplyUnverifiedNerf(int matchScore, bool isVerified, bool? allowUnverifiedArtists)
    {
        if (isVerified || allowUnverifiedArtists == true)
            return matchScore;

        var nerf = matchScore >= HighMatchThreshold ? ReducedNerf : FullNerf;
        return matchScore - nerf;
    }

    internal static int ComputeFinalScore(
        string query,
        SearchMatchCandidate candidate,
        bool? allowUnverifiedArtists)
    {
        var matchScore = ComputeMatchScore(query, candidate);
        return ApplyUnverifiedNerf(matchScore, candidate.IsVerified, allowUnverifiedArtists);
    }
}
