using Amuse.Modules.Identity.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Identity.Features.LoginPassword;

public sealed class LoginPasswordRequestValidator : AbstractValidator<LoginPasswordRequest>
{
    public LoginPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10);

        RuleFor(x => x.Context)
            .NotNull()
            .SetValidator(new PersonaContextRequestValidator());
    }
}
