using Amuse.Modules.Identity.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Identity.Features.RefreshToken;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Context)
            .NotNull()
            .SetValidator(new PersonaContextRequestValidator());
    }
}
