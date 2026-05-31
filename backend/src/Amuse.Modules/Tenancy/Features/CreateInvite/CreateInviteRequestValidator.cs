using FluentValidation;

namespace Amuse.Modules.Tenancy.Features.CreateInvite;

internal sealed class CreateInviteRequestValidator : AbstractValidator<CreateInviteRequest>
{
    public CreateInviteRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PresetRoleLabel) || x.Claims is { Count: > 0 })
            .WithMessage("Either presetRoleLabel or claims must be provided.");
    }
}
