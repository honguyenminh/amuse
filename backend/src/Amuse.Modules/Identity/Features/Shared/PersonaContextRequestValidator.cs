using Amuse.Domain.Identity;
using FluentValidation;

namespace Amuse.Modules.Identity.Features.Shared;

public sealed class PersonaContextRequestValidator : AbstractValidator<PersonaContextRequest>
{
    public PersonaContextRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();

        When(x => x.Type == PersonaContextType.Org, () =>
        {
            RuleFor(x => x.OrgId)
                .NotNull()
                .WithMessage("orgId is required when type is org.")
                .NotEqual(Guid.Empty)
                .WithMessage("orgId cannot be empty.");
        });

        When(x => x.Type == PersonaContextType.Listener, () =>
        {
            RuleFor(x => x.ListenerId)
                .NotNull()
                .WithMessage("listenerId is required when type is listener.")
                .NotEqual(Guid.Empty)
                .WithMessage("listenerId cannot be empty.");
        });

        When(x => x.Type == PersonaContextType.Platform, () =>
        {
            RuleFor(x => x.OrgId).Null().WithMessage("orgId must not be set for platform context.");
            RuleFor(x => x.ListenerId).Null().WithMessage("listenerId must not be set for platform context.");
        });
    }
}
