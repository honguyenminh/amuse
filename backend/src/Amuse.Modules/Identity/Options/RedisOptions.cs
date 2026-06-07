namespace Amuse.Modules.Identity.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;

    public string BlacklistKeyPrefix { get; set; } = "amuse:identity:blacklist:";

    public string RevokedChannel { get; set; } = "amuse:identity:token-revoked";
}
