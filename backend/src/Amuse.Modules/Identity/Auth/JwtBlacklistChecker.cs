using Amuse.Domain.Identity;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Identity.Auth;

internal static class JwtBlacklistChecker
{
    public const string RevokedFailureMessage = "identity.token_revoked";

    public static async Task<bool> IsAccessTokenRevokedAsync(
        IdentityDbContext dbContext,
        IClock clock,
        string jti,
        CancellationToken cancellationToken) =>
        await dbContext.TokenBlacklistEntries
            .AsNoTracking()
            .AnyAsync(
                e => e.Jti == TokenJti.From(jti) && e.ExpiresAt > clock.UtcNow,
                cancellationToken);
}
