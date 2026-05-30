using FluentValidation;

namespace Amuse.Modules.Identity.Features.RegisterPassword;

public sealed class RegisterPasswordRequestValidator : AbstractValidator<RegisterPasswordRequest>
{
    public RegisterPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10);

        RuleFor(x => x.Portal).IsInEnum();
    }
}
