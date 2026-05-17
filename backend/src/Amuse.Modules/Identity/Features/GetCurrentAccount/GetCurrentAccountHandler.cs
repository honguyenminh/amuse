using System.Security.Claims;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Auth;

namespace Amuse.Modules.Identity.Features.GetCurrentAccount;

internal sealed class GetCurrentAccountHandler(AccountLinker accountLinker)
{
    public async Task<Result<GetCurrentAccountResponse>> HandleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var accountGuid))
            return Result<GetCurrentAccountResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        var account = await accountLinker.GetByIdAsync(AccountId.From(accountGuid), cancellationToken);
        if (account is null)
            return Result<GetCurrentAccountResponse>.Failure(IdentityErrors.InvalidRefreshToken);

        return Result<GetCurrentAccountResponse>.Success(new GetCurrentAccountResponse(
            account.Id.Value,
            account.IdpIssuer.Value,
            account.IdpSubject.Value,
            account.Status.ToString()));
    }
}

public sealed record GetCurrentAccountResponse(
    Guid AccountId,
    string IdpIssuer,
    string IdpSubject,
    string Status);
