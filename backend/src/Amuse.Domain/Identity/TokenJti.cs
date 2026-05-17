namespace Amuse.Domain.Identity;

public readonly record struct TokenJti(string Value)
{
    public static TokenJti From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Token jti cannot be empty.", nameof(value));

        return new TokenJti(value.Trim());
    }
}
