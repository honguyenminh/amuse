using System.Text.Json;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Contracts;

namespace Amuse.Modules.Platform.Features.ListOrganizationApplications;

internal static class OrganizationApplicationMapper
{
    private static readonly JsonNamingPolicy EnumNaming = JsonNamingPolicy.CamelCase;

    public static OrganizationApplicationResponse ToResponse(
        OrganizationApplicationSummary application) =>
        new(
            application.OrganizationId,
            application.DisplayName,
            ToJsonEnum(application.OrgClass),
            ToJsonEnum(application.OnboardingStatus),
            ToJsonEnum(application.TrustTier),
            application.CreatedAt,
            application.UpdatedAt,
            new OrganizationApplicationOwner(
                application.Owner.AccountId,
                application.Owner.Email,
                application.Owner.IdpIssuer,
                application.Owner.IdpSubject,
                application.Owner.AccountStatus));

    private static string ToJsonEnum<T>(T value) where T : struct, Enum =>
        EnumNaming.ConvertName(value.ToString());
}
