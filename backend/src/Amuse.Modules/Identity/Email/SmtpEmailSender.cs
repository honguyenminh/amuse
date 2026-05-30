using Amuse.Modules.Identity.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Amuse.Modules.Identity.Email;

internal sealed class SmtpEmailSender(
    IOptions<IdentityEmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendConfirmationAsync(
        string email,
        string confirmUrl,
        CancellationToken cancellationToken)
    {
        var smtp = options.Value.Smtp;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Confirm your Amuse account";

        var body = new BodyBuilder
        {
            TextBody =
                "Confirm your email address to finish creating your Amuse account.\n\n" +
                confirmUrl +
                "\n\nIf you did not sign up, you can ignore this message.",
            HtmlBody =
                "<p>Confirm your email address to finish creating your Amuse account.</p>" +
                $"<p><a href=\"{confirmUrl}\">Confirm email</a></p>" +
                "<p>If you did not sign up, you can ignore this message.</p>",
        };
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = smtp.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation(
            "Sent confirmation email to {Email} via SMTP {Host}:{Port}. Link: {ConfirmUrl}",
            email,
            smtp.Host,
            smtp.Port,
            confirmUrl);
    }
}
