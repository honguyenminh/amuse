namespace Amuse.Modules.Identity.Features.ConfirmEmail;

public sealed record ConfirmEmailRequest(Guid UserId, string Token);
