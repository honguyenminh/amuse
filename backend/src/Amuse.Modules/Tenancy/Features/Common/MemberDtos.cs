namespace Amuse.Modules.Tenancy.Features.Common;

public sealed record OrganizationMemberResponse(
    Guid Id,
    Guid AccountId,
    string? Email,
    string? DisplayName,
    int? AvatarAccentSeed,
    string? AvatarUrl,
    string Status,
    string? PresetRoleLabel,
    IReadOnlyList<string> Claims,
    bool IsOwner,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? LastActiveAt);

public sealed record OrganizationMemberListResponse(
    IReadOnlyList<OrganizationMemberResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int PendingInviteCount);

public sealed record OrganizationInviteResponse(
    Guid Id,
    string Email,
    string? PresetRoleLabel,
    IReadOnlyList<string> Claims,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record CreateOrganizationInviteRequest(
    string Email,
    string? PresetRoleLabel,
    IReadOnlyList<string>? Claims);

public sealed record UpdateOrganizationMemberRequest(
    string? PresetRoleLabel,
    IReadOnlyList<string>? Claims);

public sealed record TransferOwnershipRequest(Guid TargetMemberId);

public sealed record ClaimPresetResponse(
    string Label,
    string DisplayName,
    string Description,
    string Icon,
    IReadOnlyList<string> Claims);

public sealed record InvitePreviewResponse(
    Guid OrganizationId,
    string OrganizationDisplayName,
    string Email,
    string Status,
    DateTimeOffset ExpiresAt);

public sealed record AcceptInviteResponse(
    Guid OrganizationId,
    Guid MemberId);

public sealed record CreateOrganizationInviteResponse(
    Guid InviteId,
    DateTimeOffset ExpiresAt);
