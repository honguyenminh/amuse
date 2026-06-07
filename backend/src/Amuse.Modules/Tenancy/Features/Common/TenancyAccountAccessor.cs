using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Modules.Tenancy.Features.Common;

internal static class TenancyAccountAccessor
{
    public static Result<AccountId> GetAccountId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var accountGuid))
            return Result<AccountId>.Failure(IdentityErrors.InvalidRefreshToken);

        return Result<AccountId>.Success(AccountId.From(accountGuid));
    }
}
