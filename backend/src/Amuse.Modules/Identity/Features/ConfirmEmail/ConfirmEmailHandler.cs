using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Amuse.Modules.Identity.Features.ConfirmEmail;

internal sealed class ConfirmEmailHandler(UserManager<ApplicationUser> userManager)
{
    public async Task<Result> HandleAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
            return Result.Failure(IdentityErrors.InvalidConfirmationToken);

        if (user.EmailConfirmed)
            return Result.Success();

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            return Result.Failure(IdentityErrors.InvalidConfirmationToken);

        return Result.Success();
    }
}
