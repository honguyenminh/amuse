using System.Text.Json;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Tenancy.Features.Shared;

public sealed record OrganizationCapabilitiesDto(
    bool CanReadOrg,
    bool CanReadMembership,
    bool CanUpload,
    bool CanWriteDraft,
    bool CanPublishPublic,
    bool CanReadPayout,
    IReadOnlyList<string> ClaimStrings);

public sealed record OrganizationResponse(
    Guid Id,
    string DisplayName,
    string OrgClass,
    string LifecycleStatus,
    string OnboardingStatus,
    string TrustTier,
    DateTimeOffset? ApprovedAt,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    OrganizationCapabilitiesDto Capabilities,
    bool IsOwner);

internal static class OrganizationDtoMapper
{
    private static readonly JsonNamingPolicy EnumNaming = JsonNamingPolicy.CamelCase;

    public static OrganizationCapabilitiesDto ToDto(OrgCapabilities capabilities) =>
        new(
            capabilities.CanReadOrg,
            capabilities.CanReadMembership,
            capabilities.CanUpload,
            capabilities.CanWriteDraft,
            capabilities.CanPublishPublic,
            capabilities.CanReadPayout,
            capabilities.ToClaimStrings());

    public static OrganizationResponse ToResponse(Organization organization, bool isOwner)
    {
        var capabilities = organization.EvaluateCapabilities();
        return new OrganizationResponse(
            organization.Id.Value,
            organization.DisplayName,
            ToJsonEnum(organization.OrgClass),
            ToJsonEnum(organization.LifecycleStatus),
            ToJsonEnum(organization.OnboardingStatus),
            ToJsonEnum(organization.TrustTier),
            organization.ApprovedAt,
            organization.RejectionReason,
            organization.CreatedAt,
            organization.UpdatedAt,
            ToDto(capabilities),
            isOwner);
    }

    private static string ToJsonEnum<T>(T value) where T : struct, Enum =>
        EnumNaming.ConvertName(value.ToString());
}
