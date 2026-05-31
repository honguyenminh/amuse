namespace Amuse.Modules.Tenancy.Features.CreateInvite;

public sealed record CreateInviteRequest(
    string Email,
    string? PresetRoleLabel,
    IReadOnlyList<string>? Claims);
