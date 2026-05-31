using Amuse.Modules.Tenancy.Contracts;

namespace Amuse.Modules.Identity.Email;

internal sealed class TenancyInviteEmailSender(IEmailSender emailSender) : ITenancyInviteEmailSender
{
    public Task SendOrganizationInviteAsync(
        string email,
        string organizationDisplayName,
        string inviteUrl,
        CancellationToken cancellationToken) =>
        emailSender.SendOrganizationInviteAsync(email, organizationDisplayName, inviteUrl, cancellationToken);
}
