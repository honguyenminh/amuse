namespace Amuse.Modules.Identity.Options;

public sealed class IdentityEmailOptions
{
    public const string SectionName = "Identity:Email";

    public bool RequireConfirmation { get; set; } = true;

    public string ConsumerAppBaseUrl { get; set; } = "http://localhost:3000";

    public string BusinessAppBaseUrl { get; set; } = "http://localhost:3001";

    public int ConfirmationTokenHours { get; set; } = 24;

    public int ResendCooldownSeconds { get; set; } = 60;

    public IdentitySmtpOptions Smtp { get; set; } = new();
}
