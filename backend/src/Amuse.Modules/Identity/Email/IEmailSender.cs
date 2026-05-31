namespace Amuse.Modules.Identity.Email;

public interface IEmailSender
{
    Task SendConfirmationAsync(string email, string confirmUrl, CancellationToken cancellationToken);

    Task SendOrganizationInviteAsync(
        string email,
        string organizationDisplayName,
        string inviteUrl,
        CancellationToken cancellationToken);
}
