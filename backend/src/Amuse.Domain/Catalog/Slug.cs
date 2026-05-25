using System.Text.RegularExpressions;

namespace Amuse.Domain.Catalog;

/// <summary>
/// URL-safe lowercase identifier: <c>[a-z0-9](-[a-z0-9]+)*</c>, length 1..<see cref="MaxLength"/>.
/// </summary>
public readonly partial record struct Slug
{
    public const int MaxLength = 96;

    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug From(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException(
                $"Invalid slug '{value}'. Must be lowercase alphanumerics separated by single hyphens.",
                nameof(value));
        return new Slug(value);
    }

    public static bool IsValid(string? value) =>
        value is not null
        && value.Length is > 0 and <= MaxLength
        && SlugRegex().IsMatch(value);

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();
}
