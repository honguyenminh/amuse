using Amuse.Modules.Identity.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Identity.Features.ExternalLoginComplete;

public sealed class ExternalLoginCompleteRequestValidator : AbstractValidator<ExternalLoginCompleteRequest>
{
    public ExternalLoginCompleteRequestValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(64);
        RuleFor(x => x.GrantType).IsInEnum();

        When(x => x.GrantType == ExternalLoginGrantType.AuthorizationCode, () =>
        {
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.CodeVerifier).NotEmpty();
            RuleFor(x => x.RedirectUri).NotEmpty().Must(BeAbsoluteUri);
        });

        When(x => x.GrantType == ExternalLoginGrantType.IdToken, () =>
        {
            RuleFor(x => x.IdToken).NotEmpty();
        });

        RuleFor(x => x.Context)
            .NotNull()
            .SetValidator(new PersonaContextRequestValidator());
    }

    private static bool BeAbsoluteUri(string? uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out _);
}
