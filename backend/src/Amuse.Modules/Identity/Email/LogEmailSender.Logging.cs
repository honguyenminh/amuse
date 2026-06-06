using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Identity.Email;

internal sealed partial class LogEmailSender
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email confirmation for {Email}. Open this link to confirm: {ConfirmUrl}")]
    private partial void LogEmailConfirmation(string email, string confirmUrl);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Organization invite for {Email} to {Organization}. Open: {InviteUrl}")]
    private partial void LogOrganizationInvite(string email, string organization, string inviteUrl);
}
