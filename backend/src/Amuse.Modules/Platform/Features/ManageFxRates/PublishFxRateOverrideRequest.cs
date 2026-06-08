namespace Amuse.Modules.Platform.Features.ManageFxRates;

public sealed record PublishFxRateOverrideRequest(
    string QuoteCurrency,
    decimal Rate,
    DateTimeOffset EffectiveAt);
