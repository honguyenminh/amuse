using Amuse.Domain.Catalog;

namespace Amuse.Modules.Catalog.Tests;

public class CatalogFormattedTextTests
{
    [Fact]
    public void TryCreate_accepts_null()
    {
        var result = CatalogFormattedText.TryCreate(null);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void TryCreate_accepts_plain_text()
    {
        var result = CatalogFormattedText.TryCreate("  Hello\nworld  ");
        Assert.True(result.IsSuccess);
        Assert.Equal("Hello\nworld", result.Value!.Value.Value);
    }

    [Fact]
    public void TryCreate_accepts_limited_markdown()
    {
        var result = CatalogFormattedText.TryCreate("**bold** *italic* `code` [link](https://example.com) #electronic");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void TryCreate_rejects_html()
    {
        var result = CatalogFormattedText.TryCreate("<script>alert(1)</script>");
        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidFormattedText, result.Error);
    }

    [Fact]
    public void TryCreate_rejects_unsafe_links()
    {
        var result = CatalogFormattedText.TryCreate("[x](javascript:alert(1))");
        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidFormattedText, result.Error);
    }

    [Fact]
    public void TryCreate_rejects_markdown_headings()
    {
        var result = CatalogFormattedText.TryCreate("# Heading");
        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidFormattedText, result.Error);
    }

    [Fact]
    public void TryCreate_rejects_excessive_length()
    {
        var result = CatalogFormattedText.TryCreate(new string('a', CatalogFormattedText.MaxLength + 1));
        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.InvalidFormattedText, result.Error);
    }

    [Fact]
    public void Normalize_collapses_excessive_blank_lines()
    {
        var normalized = CatalogFormattedText.Normalize("a\n\n\n\nb");
        Assert.Equal("a\n\nb", normalized);
    }
}
