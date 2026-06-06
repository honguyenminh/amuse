using System.Text.RegularExpressions;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Catalog;

public readonly record struct CatalogFormattedText
{
    public const int MaxLength = 4000;

    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MarkdownLinkRegex = new(@"\[([^\]]*)\]\(([^)]*)\)", RegexOptions.Compiled);
    private static readonly Regex HashtagRegex = new(@"(?<![\w])#([a-zA-Z][a-zA-Z0-9_]{0,63})", RegexOptions.Compiled);

    private static readonly string[] UnsupportedBlockLinePrefixes =
    [
        "# ",
        "> ",
        "- ",
        "* ",
        "![",
        "```",
    ];

    public string Value { get; }

    private CatalogFormattedText(string value) => Value = value;

    public static Result<CatalogFormattedText?> TryCreate(string? raw)
    {
        if (raw is null)
            return Result<CatalogFormattedText?>.Success(null);

        var normalized = Normalize(raw);
        if (normalized.Length == 0)
            return Result<CatalogFormattedText?>.Success(null);

        if (normalized.Length > MaxLength)
            return Result<CatalogFormattedText?>.Failure(CatalogErrors.InvalidFormattedText);

        if (HtmlTagRegex.IsMatch(normalized))
            return Result<CatalogFormattedText?>.Failure(CatalogErrors.InvalidFormattedText);

        if (ContainsUnsupportedBlockMarkdown(normalized))
            return Result<CatalogFormattedText?>.Failure(CatalogErrors.InvalidFormattedText);

        if (!ValidateLinks(normalized))
            return Result<CatalogFormattedText?>.Failure(CatalogErrors.InvalidFormattedText);

        if (!ValidateHashtags(normalized))
            return Result<CatalogFormattedText?>.Failure(CatalogErrors.InvalidFormattedText);

        return Result<CatalogFormattedText?>.Success(new CatalogFormattedText(normalized));
    }

    public static string? ToStoredValue(CatalogFormattedText? formattedText) =>
        formattedText?.Value;

    public static string Normalize(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        var normalized = trimmed.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        normalized = Regex.Replace(normalized, "\n{3,}", "\n\n");
        return normalized;
    }

    private static bool ContainsUnsupportedBlockMarkdown(string value)
    {
        foreach (var line in value.Split('\n'))
        {
            var trimmedLine = line.TrimStart();
            foreach (var prefix in UnsupportedBlockLinePrefixes)
            {
                if (trimmedLine.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }

            if (Regex.IsMatch(trimmedLine, @"^\d+\.\s"))
                return true;
        }

        return false;
    }

    private static bool ValidateLinks(string value)
    {
        foreach (Match match in MarkdownLinkRegex.Matches(value))
        {
            var url = match.Groups[2].Value.Trim();
            if (!IsAllowedHttpUrl(url))
                return false;
        }

        return true;
    }

    private static bool ValidateHashtags(string value)
    {
        foreach (Match match in HashtagRegex.Matches(value))
        {
            var tag = match.Groups[1].Value;
            if (tag.Length is 0 or > 64)
                return false;
        }

        return true;
    }

    private static bool IsAllowedHttpUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return false;

        return parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps;
    }
}
