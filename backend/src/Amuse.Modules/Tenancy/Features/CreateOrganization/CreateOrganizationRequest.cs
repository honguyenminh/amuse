using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Tenancy.Features.CreateOrganization;

public sealed record CreateOrganizationRequest(
    string DisplayName,
    OrganizationClass OrgClass,
    string? Description = null,
    string? WebsiteUrl = null,
    string? CountryCode = null,
    string? ImprintName = null,
    bool CreateDefaultArtist = true);
