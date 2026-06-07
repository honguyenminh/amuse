using System.Security.Cryptography;
using System.Text;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tenancy;

public sealed class OrganizationInvite
{
    public const int MaxEmailLength = 320;
    public const int TokenLengthBytes = 32;
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);

    public Guid Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public string Email { get; private set; } = null!;
    public AccountId InvitedByAccountId { get; private set; }
    public string? PresetRoleLabel { get; private set; }
    public IReadOnlyList<string> Claims { get; private set; } = [];
    public string TokenHash { get; private set; } = null!;
    public OrganizationInviteStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public AccountId? AcceptedByAccountId { get; private set; }

    private OrganizationInvite()
    {
    }

    public bool IsPending => Status == OrganizationInviteStatus.Pending;

    public bool IsExpired(DateTimeOffset now) =>
        Status == OrganizationInviteStatus.Pending && now >= ExpiresAt;

    public Result EnsurePending(DateTimeOffset now)
    {
        if (Status != OrganizationInviteStatus.Pending)
            return Result.Failure(TenancyErrors.InvalidInviteTransition);

        if (now >= ExpiresAt)
        {
            Status = OrganizationInviteStatus.Expired;
            return Result.Failure(TenancyErrors.InviteExpired);
        }

        return Result.Success();
    }

    public static Result<(OrganizationInvite Invite, string RawToken)> CreatePending(
        OrganizationId organizationId,
        string email,
        AccountId invitedByAccountId,
        string? presetRoleLabel,
        IReadOnlyList<string> claims,
        DateTimeOffset now,
        TimeSpan? ttl = null)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail is null)
            return Result<(OrganizationInvite, string)>.Failure(TenancyErrors.InvalidInviteEmail);

        if (!OrgClaimPresets.TryResolveClaims(presetRoleLabel, claims, out var resolvedClaims))
            return Result<(OrganizationInvite, string)>.Failure(TenancyErrors.InvalidClaim);

        var rawTokenBytes = RandomNumberGenerator.GetBytes(TokenLengthBytes);
        var rawToken = Convert.ToBase64String(rawTokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var invite = new OrganizationInvite
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Email = normalizedEmail,
            InvitedByAccountId = invitedByAccountId,
            PresetRoleLabel = string.IsNullOrWhiteSpace(presetRoleLabel) ? null : presetRoleLabel.Trim(),
            Claims = resolvedClaims,
            TokenHash = HashToken(rawToken),
            Status = OrganizationInviteStatus.Pending,
            ExpiresAt = now.Add(ttl ?? DefaultTtl),
            CreatedAt = now,
        };

        return Result<(OrganizationInvite, string)>.Success((invite, rawToken));
    }

    public Result Revoke(DateTimeOffset now)
    {
        if (Status != OrganizationInviteStatus.Pending)
            return Result.Failure(TenancyErrors.InvalidInviteTransition);

        Status = OrganizationInviteStatus.Revoked;
        return Result.Success();
    }

    public Result Decline(DateTimeOffset now)
    {
        var pending = EnsurePending(now);
        if (!pending.IsSuccess)
            return pending;

        Status = OrganizationInviteStatus.Revoked;
        return Result.Success();
    }

    public Result MarkExpired(DateTimeOffset now)
    {
        if (Status != OrganizationInviteStatus.Pending)
            return Result.Failure(TenancyErrors.InvalidInviteTransition);

        Status = OrganizationInviteStatus.Expired;
        return Result.Success();
    }

    public Result Accept(AccountId accountId, string accountEmail, DateTimeOffset now)
    {
        if (Status != OrganizationInviteStatus.Pending)
            return Result.Failure(TenancyErrors.InvalidInviteTransition);

        if (now >= ExpiresAt)
        {
            Status = OrganizationInviteStatus.Expired;
            return Result.Failure(TenancyErrors.InviteExpired);
        }

        var normalizedAccountEmail = NormalizeEmail(accountEmail);
        if (normalizedAccountEmail is null || !string.Equals(Email, normalizedAccountEmail, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(TenancyErrors.InviteEmailMismatch);

        Status = OrganizationInviteStatus.Accepted;
        AcceptedAt = now;
        AcceptedByAccountId = accountId;
        return Result.Success();
    }

    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string? NormalizeEmail(string? email)
    {
        var trimmed = (email ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > MaxEmailLength)
            return null;

        return trimmed.ToLowerInvariant();
    }
}
