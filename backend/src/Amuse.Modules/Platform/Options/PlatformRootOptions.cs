namespace Amuse.Modules.Platform.Options;

public sealed class PlatformRootOptions
{
    public const string SectionName = "Platform:Root";

    public Guid AccountId { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }

    // Empty default is intentional: the .NET configuration binder APPENDS to an existing
    // default array rather than replacing it, which duplicates entries when config also
    // supplies the same claim. Source of truth is appsettings*.json.
    public string[] Claims { get; set; } = [];
}
