namespace Amuse.Domain.Billing;

public sealed class BannedPaymentInstrument
{
    public BannedPaymentInstrumentId Id { get; private set; }
    public string PaymentMethodFingerprint { get; private set; } = null!;
    public string? Reason { get; private set; }
    public DateTimeOffset BannedAt { get; private set; }

    private BannedPaymentInstrument()
    {
    }

    public static BannedPaymentInstrument Create(string paymentMethodFingerprint, string? reason, DateTimeOffset bannedAt)
    {
        var fingerprint = paymentMethodFingerprint.Trim();
        if (fingerprint.Length == 0)
            throw new ArgumentException("Payment method fingerprint is required.", nameof(paymentMethodFingerprint));

        return new BannedPaymentInstrument
        {
            Id = BannedPaymentInstrumentId.New(),
            PaymentMethodFingerprint = fingerprint,
            Reason = reason?.Trim(),
            BannedAt = bannedAt,
        };
    }
}
