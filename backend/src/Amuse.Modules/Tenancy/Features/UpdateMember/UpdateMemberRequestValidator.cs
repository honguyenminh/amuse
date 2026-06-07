using Amuse.Modules.Tenancy.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Tenancy.Features.UpdateMember;

internal sealed class UpdateMemberRequestValidator : AbstractValidator<UpdateOrganizationMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PresetRoleLabel) || x.Claims is { Count: > 0 })
            .WithMessage("Either presetRoleLabel or claims must be provided.");
    }
}
