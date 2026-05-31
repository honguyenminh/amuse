using Amuse.Modules.Identity.Email;

namespace Amuse.Api.IntegrationTests;

public sealed class CaptureEmailSender : IEmailSender
{
    public string? LastEmail { get; private set; }

    public string? LastConfirmUrl { get; private set; }

    public string? LastInviteUrl { get; private set; }

    public string? LastInviteOrganization { get; private set; }

    public Task SendConfirmationAsync(
        string email,
        string confirmUrl,
        CancellationToken cancellationToken)
    {
        LastEmail = email;
        LastConfirmUrl = confirmUrl;
        return Task.CompletedTask;
    }

    public Task SendOrganizationInviteAsync(
        string email,
        string organizationDisplayName,
        string inviteUrl,
        CancellationToken cancellationToken)
    {
        LastEmail = email;
        LastInviteOrganization = organizationDisplayName;
        LastInviteUrl = inviteUrl;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        LastEmail = null;
        LastConfirmUrl = null;
        LastInviteUrl = null;
        LastInviteOrganization = null;
    }
}
