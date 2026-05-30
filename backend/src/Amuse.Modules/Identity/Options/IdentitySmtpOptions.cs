namespace Amuse.Modules.Identity.Options;

public sealed class IdentitySmtpOptions
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public bool UseSsl { get; set; }

    public string FromAddress { get; set; } = "noreply@amuse.local";

    public string FromName { get; set; } = "Amuse";
}
