using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Audit;
using Amuse.Modules.Audit.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Features.RevokeToken;

internal sealed class RevokeTokenHandler(
    IdentityDbContext dbContext,
    IAuditWriter auditWriter,
    IClock clock)
{
    public async Task<Result> HandleAsync(
        string? refreshToken,
        string? authorizationHeader,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var changed = false;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = TokenIssuer.HashRefreshToken(refreshToken);
            var session = await dbContext.RefreshSessions
                .FirstOrDefaultAsync(s => s.TokenHash == hash, cancellationToken);

            if (session is not null)
            {
                session.Revoke(now);
                changed = true;
            }
        }

        if (AccessTokenClaims.TryReadJtiAndExpiry(authorizationHeader, out var jti, out var expiresAt))
        {
            var alreadyListed = await dbContext.TokenBlacklistEntries
                .AnyAsync(e => e.Jti == TokenJti.From(jti), cancellationToken);

            if (!alreadyListed)
            {
                dbContext.TokenBlacklistEntries.Add(
                    TokenBlacklistEntry.Create(TokenJti.From(jti), expiresAt, "revoke"));
                changed = true;
            }
        }

        if (changed)
            await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            Action = "Update",
            TableName = "identity.refresh_session",
            TargetId = Guid.Empty,
            ChangedAt = now,
            Reason = "refresh_revoked",
        }, cancellationToken);

        return Result.Success();
    }
}
