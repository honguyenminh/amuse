using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Tenancy.Features.Common;

internal sealed record OrganizationAuditSnapshot(
    Guid Id,
    string DisplayName,
    string? Description,
    string? WebsiteUrl,
    string? CountryCode,
    string? ImprintName);

internal static class TenancyAuditSnapshotMapper
{
    internal static OrganizationAuditSnapshot FromOrganization(Organization organization) =>
        new(
            organization.Id.Value,
            organization.DisplayName,
            organization.Description,
            organization.WebsiteUrl,
            organization.CountryCode,
            organization.ImprintName);
}
