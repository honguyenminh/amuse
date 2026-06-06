using System.Text.RegularExpressions;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Discovery;

public readonly record struct ShareGrantEmail(string Value)
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static Result<ShareGrantEmail> TryCreate(string? raw)
    {
        var trimmed = (raw ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmed.Length is 0 or > 320 || !EmailPattern.IsMatch(trimmed))
            return Result<ShareGrantEmail>.Failure(DiscoveryErrors.InvalidShareEmail);

        return Result<ShareGrantEmail>.Success(new ShareGrantEmail(trimmed));
    }
}
