using System.Security.Claims;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Platform.Features.Common;
using Amuse.Modules.Tenancy.Contracts;
using Amuse.Modules.Tenancy.Features.Common;

namespace Amuse.Modules.Platform.Features.ForceTransferOwnership;

internal sealed class ForceTransferOwnershipHandler(
    IOrganizationLifecycleCommands lifecycleCommands,
    IAuditWriter auditWriter,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        Guid organizationId,
        TransferOwnershipRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accountResult = PlatformAccountAccessor.GetAccountId(principal);
        if (!accountResult.IsSuccess)
            return Result.Failure(accountResult.Error!);

        if (organizationId == Guid.Empty)
            return Result.Failure(TenancyErrors.OrganizationNotFound);

        var result = await lifecycleCommands.ForceTransferOwnershipAsync(
            OrganizationId.From(organizationId),
            request.TargetMemberId,
            cancellationToken);

        if (!result.IsSuccess)
            return result;

        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "organization_ownership_force_transferred",
            TableName = "tenancy.organization_member",
            TargetId = request.TargetMemberId,
            ChangedAt = clock.UtcNow,
            ActorAccountId = accountResult.Value!.Value,
        }, cancellationToken);

        return Result.Success();
    }
}
