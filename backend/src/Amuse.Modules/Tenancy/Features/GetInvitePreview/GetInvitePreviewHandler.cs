using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Tenancy.Features.Shared;
using Amuse.Modules.Tenancy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Features.GetInvitePreview;

internal sealed class GetInvitePreviewHandler(TenancyDbContext dbContext, IClock clock)
{
    public async Task<Result<InvitePreviewResponse>> HandleAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<InvitePreviewResponse>.Failure(TenancyErrors.InviteNotFound);

        var tokenHash = OrganizationInvite.HashToken(token);
        var invite = await dbContext.OrganizationInvites.AsNoTracking()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
        if (invite is null)
            return Result<InvitePreviewResponse>.Failure(TenancyErrors.InviteNotFound);

        if (invite.IsExpired(clock.UtcNow))
            return Result<InvitePreviewResponse>.Failure(TenancyErrors.InviteExpired);

        var organization = await dbContext.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == invite.OrganizationId, cancellationToken);
        if (organization is null)
            return Result<InvitePreviewResponse>.Failure(TenancyErrors.OrganizationNotFound);

        return Result<InvitePreviewResponse>.Success(new InvitePreviewResponse(
            organization.Id.Value,
            organization.DisplayName,
            invite.Email,
            invite.Status.ToString(),
            invite.ExpiresAt));
    }
}
