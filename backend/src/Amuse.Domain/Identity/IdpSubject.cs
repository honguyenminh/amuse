namespace Amuse.Domain.Identity;

public sealed class IdpSubject : IEquatable<IdpSubject>
{
    public const int MaxLength = 512;

    public string Value { get; }

    private IdpSubject(string value) => Value = value;

    public static IdpSubject From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IdP subject cannot be empty.", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
            throw new ArgumentException($"IdP subject cannot exceed {MaxLength} characters.", nameof(value));

        return new IdpSubject(trimmed);
    }

    public bool Equals(IdpSubject? other) =>
        other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is IdpSubject other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;
}
