using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Platform.Features.Shared;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Features.Shared;

namespace Amuse.Modules.Platform.Features.RecoverOrganization;

internal sealed class RecoverOrganizationHandler(
    IOrganizationLifecycleCommands lifecycleCommands,
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

        if (organizationId == Guid.Empty)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var result = await lifecycleCommands.RecoverClosedOrganizationAsync(
            OrganizationId.From(organizationId),
            cancellationToken);

        if (!result.IsSuccess)
            return result;

        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "organization_recovered",
            TableName = "tenancy.organization",
            TargetId = organizationId,
            ChangedAt = clock.UtcNow,
            ActorAccountId = accountResult.Value!.Value,
        }, cancellationToken);

        return Result.Success();
    }
}
