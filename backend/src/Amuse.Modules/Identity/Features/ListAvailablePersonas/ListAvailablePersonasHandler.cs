using System.Security.Claims;
using System.Text.Json;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Contracts;

namespace Amuse.Modules.Identity.Features.ListAvailablePersonas;

internal sealed class ListAvailablePersonasHandler(
    ITenancyPersonaReadModel tenancyReadModel,
    IListenerPersonaReadModel listenerReadModel,
    IPlatformPersonaReadModel platformReadModel)
{
    public async Task<Result<IReadOnlyList<AvailablePersona>>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var accountGuid))
            return Result<IReadOnlyList<AvailablePersona>>.Failure(IdentityErrors.InvalidRefreshToken);

        var accountId = AccountId.From(accountGuid);
        var personas = new List<AvailablePersona>();

        var orgs = await tenancyReadModel.ListAvailableOrgsAsync(accountId, cancellationToken);
        personas.AddRange(orgs.Select(o => new AvailablePersona(
            "org",
            o.OrganizationId,
            null,
            o.DisplayName,
            ToJsonEnum(o.OrgClass),
            ToJsonEnum(o.OnboardingStatus))));

        var listenerId = await listenerReadModel.GetProfileIdForAccountAsync(accountId, cancellationToken);
        if (listenerId is not null)
            personas.Add(new AvailablePersona("listener", null, listenerId.Value.Value, "Listener"));

        if (await platformReadModel.IsPlatformOperatorAsync(accountId, cancellationToken))
            personas.Add(new AvailablePersona("platform", null, null, "Platform"));

        return Result<IReadOnlyList<AvailablePersona>>.Success(personas);
    }

    private static string ToJsonEnum<T>(T value) where T : struct, Enum =>
        JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}
