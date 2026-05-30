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
using Amuse.Modules.Tenancy.Features.Shared;

namespace Amuse.Modules.Platform.Features.ApproveOrganization;

internal sealed class ApproveOrganizationHandler(
    IOrganizationLifecycleCommands lifecycleCommands,
    IPlatformOperatorLookup operatorLookup,
    IAuditWriter auditWriter,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid organizationId,
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

        var result = await lifecycleCommands.ApproveBackingOrganizationAsync(
            OrganizationId.From(organizationId),
            operatorId.Value,
            cancellationToken);

        if (!result.IsSuccess)
            return result;

        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "organization_approved",
            TableName = "tenancy.organization",
            TargetId = organizationId,
            ChangedAt = clock.UtcNow,
            ActorAccountId = accountResult.Value!.Value,
        }, cancellationToken);

        return Result.Success();
    }
}
