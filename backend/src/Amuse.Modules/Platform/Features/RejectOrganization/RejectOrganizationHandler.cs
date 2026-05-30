using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Platform.Contracts;
using Amuse.Modules.Platform.Features.Shared;
using Amuse.Modules.Tenancy.Contracts;

namespace Amuse.Modules.Platform.Features.RejectOrganization;

internal sealed class RejectOrganizationHandler(
    IOrganizationLifecycleCommands lifecycleCommands,
    IPlatformOperatorLookup operatorLookup,
    IAuditWriter auditWriter,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid organizationId,
        RejectOrganizationRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = PlatformAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result.Failure(accountResult.Error!);

        var operatorId = await operatorLookup.GetOperatorIdForAccountAsync(
            accountResult.Value!,
            cancellationToken);

        if (operatorId is null)
            return Result.Failure(IdentityErrors.InvalidPersonaContext);

        if (organizationId == Guid.Empty)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var result = await lifecycleCommands.RejectBackingOrganizationAsync(
            OrganizationId.From(organizationId),
            request.Reason,
            cancellationToken);

        if (!result.IsSuccess)
            return result;

        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "organization_rejected",
            TableName = "tenancy.organization",
            TargetId = organizationId,
            ChangedAt = clock.UtcNow,
            ActorAccountId = accountResult.Value!.Value,
            Reason = request.Reason,
        }, cancellationToken);

        return Result.Success();
    }
}
