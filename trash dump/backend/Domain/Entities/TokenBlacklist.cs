namespace Amuse.Api.Domain.Entities;

public sealed class TokenBlacklist
{
    public string Jti { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? Reason { get; set; }
}
