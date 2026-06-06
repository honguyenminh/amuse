using Microsoft.Extensions.Logging;

namespace Amuse.Modules.Identity.Email;

internal sealed partial class SmtpEmailSender
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Sent confirmation email to {Email} via SMTP {Host}:{Port}. Link: {ConfirmUrl}")]
    private partial void LogConfirmationSent(string email, string host, int port, string confirmUrl);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Sent organization invite to {Email} for {Organization} via SMTP {Host}:{Port}. Link: {InviteUrl}")]
    private partial void LogOrganizationInviteSent(
        string email,
        string organization,
        string host,
        int port,
        string inviteUrl);
}
