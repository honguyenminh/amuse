using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Identity.Email;

internal sealed partial class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendConfirmationAsync(
        string email,
        string confirmUrl,
        CancellationToken cancellationToken)
    {
        LogEmailConfirmation(email, confirmUrl);
        return Task.CompletedTask;
    }

    public Task SendOrganizationInviteAsync(
        string email,
        string organizationDisplayName,
        string inviteUrl,
        CancellationToken cancellationToken)
    {
        LogOrganizationInvite(email, organizationDisplayName, inviteUrl);
        return Task.CompletedTask;
    }
}
