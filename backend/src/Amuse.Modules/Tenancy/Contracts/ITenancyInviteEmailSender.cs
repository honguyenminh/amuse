namespace Amuse.Modules.Tenancy.Contracts;

public interface ITenancyInviteEmailSender
{
    Task SendOrganizationInviteAsync(
        string email,
        string organizationDisplayName,
        string inviteUrl,
        CancellationToken cancellationToken);
}
