namespace Amuse.Api.Domain.Entities;

public sealed class RefreshSession
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string SessionHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
}
