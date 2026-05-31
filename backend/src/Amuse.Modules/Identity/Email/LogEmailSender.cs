using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Identity.Email;

internal sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendConfirmationAsync(
        string email,
        string confirmUrl,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email confirmation for {Email}. Open this link to confirm: {ConfirmUrl}",
            email,
            confirmUrl);
        return Task.CompletedTask;
    }

    public Task SendOrganizationInviteAsync(
        string email,
        string organizationDisplayName,
        string inviteUrl,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Organization invite for {Email} to {Organization}. Open: {InviteUrl}",
            email,
            organizationDisplayName,
            inviteUrl);
        return Task.CompletedTask;
    }
}
