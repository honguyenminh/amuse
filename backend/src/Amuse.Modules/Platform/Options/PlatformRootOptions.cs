namespace Amuse.Modules.Platform.Options;

public sealed class PlatformRootOptions
{
    public const string SectionName = "Platform:Root";

    public Guid AccountId { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string[] Claims { get; set; } = ["platform:admin"];
}
