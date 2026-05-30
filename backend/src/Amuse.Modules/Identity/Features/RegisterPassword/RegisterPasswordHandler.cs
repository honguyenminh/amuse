using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Auth;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Email;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Features.RegisterPassword;

internal sealed class RegisterPasswordHandler(
    UserManager<ApplicationUser> userManager,
    AccountLinker accountLinker,
    IListenerPersonaReadModel listenerReadModel,
    IEmailSender emailSender,
    EmailConfirmationLinkBuilder linkBuilder,
    IOptions<IdentityEmailOptions> emailOptions)
{
    public async Task<Result<RegisterPasswordResponse>> HandleAsync(
        RegisterPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userManager.NormalizeEmail(request.Email);
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Result<RegisterPasswordResponse>.Failure(IdentityErrors.EmailAlreadyRegistered);

        var userId = Guid.CreateVersion7();
        var account = await accountLinker.GetOrCreateAsync(
            IdpIssuer.From(AuthConstants.LocalIdpIssuer),
            IdpSubject.From(userId.ToString()),
            cancellationToken);

        var ensureListener = await listenerReadModel.EnsureProfileForAccountAsync(account.Id, cancellationToken);
        if (!ensureListener.IsSuccess)
            return Result<RegisterPasswordResponse>.Failure(ensureListener.Error!);

        var requireConfirmation = emailOptions.Value.RequireConfirmation;
        var user = new ApplicationUser
        {
            Id = userId,
            Email = request.Email,
            UserName = request.Email,
            NormalizedEmail = normalizedEmail,
            NormalizedUserName = normalizedEmail,
            EmailConfirmed = !requireConfirmation,
            AccountId = account.Id.Value,
        };

        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            var detail = string.Join("; ", create.Errors.Select(e => e.Description));
            return Result<RegisterPasswordResponse>.Failure(
                IdentityErrors.RegistrationFailedWithDetails(detail));
        }

        if (requireConfirmation)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmUrl = linkBuilder.Build(request.Portal, user.Id, token);
            await emailSender.SendConfirmationAsync(request.Email, confirmUrl, cancellationToken);
        }

        return Result<RegisterPasswordResponse>.Success(new RegisterPasswordResponse(
            requireConfirmation
                ? "Check your email to confirm your account before signing in."
                : "Account created. You can sign in now.",
            request.Email));
    }
}
