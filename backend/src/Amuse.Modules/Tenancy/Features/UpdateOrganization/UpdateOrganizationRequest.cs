namespace Amuse.Modules.Tenancy.Features.UpdateOrganization;

public sealed record UpdateOrganizationProfileRequest(
    string? Description = null,
    string? WebsiteUrl = null,
    string? CountryCode = null,
    string? ImprintName = null);
