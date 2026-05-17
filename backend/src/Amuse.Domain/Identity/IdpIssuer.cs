namespace Amuse.Domain.Identity;

public sealed class IdpIssuer : IEquatable<IdpIssuer>
{
    public const int MaxLength = 256;

    public string Value { get; }

    private IdpIssuer(string value) => Value = value;

    public static IdpIssuer From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IdP issuer cannot be empty.", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
            throw new ArgumentException($"IdP issuer cannot exceed {MaxLength} characters.", nameof(value));

        return new IdpIssuer(trimmed);
    }

    public bool Equals(IdpIssuer? other) =>
        other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is IdpIssuer other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;
}
