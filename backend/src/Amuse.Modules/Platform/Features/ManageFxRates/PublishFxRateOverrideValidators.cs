using FluentValidation;

namespace Amuse.Modules.Platform.Features.ManageFxRates;

public sealed class PublishFxRateOverrideRequestValidator : AbstractValidator<PublishFxRateOverrideRequest>
{
    public PublishFxRateOverrideRequestValidator()
    {
        RuleFor(request => request.QuoteCurrency)
            .NotEmpty()
            .Length(3);

        RuleFor(request => request.Rate)
            .GreaterThan(0);

        RuleFor(request => request.EffectiveAt)
            .Must(HasExplicitOffset)
            .WithMessage("effectiveAt must include a timezone offset (e.g. Z or +00:00).");
    }

    private static bool HasExplicitOffset(DateTimeOffset value) =>
        value.Offset != TimeSpan.Zero || value.ToString("O").EndsWith('Z');
}
