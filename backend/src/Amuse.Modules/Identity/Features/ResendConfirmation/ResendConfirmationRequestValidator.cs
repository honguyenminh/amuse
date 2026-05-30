using FluentValidation;

namespace Amuse.Modules.Identity.Features.ResendConfirmation;

public sealed class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
{
    public ResendConfirmationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Portal).IsInEnum();
    }
}
