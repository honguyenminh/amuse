namespace Amuse.Domain.Identity;

public readonly record struct AccountId(Guid Value)
{
    public static AccountId New() => new(Guid.CreateVersion7());

    public static AccountId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Account id cannot be empty.", nameof(value));

        return new AccountId(value);
    }

    public override string ToString() => Value.ToString();
}
