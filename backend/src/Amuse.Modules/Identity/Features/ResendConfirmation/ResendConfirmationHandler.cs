using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Identity.Email;
using Amuse.Modules.Identity.Features.RegisterPassword;
using Amuse.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Amuse.Modules.Identity.Features.ResendConfirmation;

internal sealed class ResendConfirmationHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    EmailConfirmationLinkBuilder linkBuilder,
    ConfirmationResendThrottle throttle,
    IClock clock)
{
    public async Task<Result<ResendConfirmationResponse>> HandleAsync(
        ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var throttleResult = throttle.TryAcquire(request.Email, clock.UtcNow);
        if (!throttleResult.IsSuccess)
            return Result<ResendConfirmationResponse>.Failure(throttleResult.Error!);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
        {
            return Result<ResendConfirmationResponse>.Success(new ResendConfirmationResponse(
                "If an unconfirmed account exists for this email, a confirmation message has been sent."));
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmUrl = linkBuilder.Build(request.Portal, user.Id, token);
        await emailSender.SendConfirmationAsync(request.Email, confirmUrl, cancellationToken);

        return Result<ResendConfirmationResponse>.Success(new ResendConfirmationResponse(
            "If an unconfirmed account exists for this email, a confirmation message has been sent."));
    }
}
