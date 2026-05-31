namespace Amuse.Modules.Tenancy.Options;

public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";

    public string BusinessPortalBaseUrl { get; set; } = "http://localhost:3001";
}
