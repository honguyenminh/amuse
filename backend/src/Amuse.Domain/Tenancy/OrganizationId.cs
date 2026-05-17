namespace Amuse.Domain.Tenancy;

public readonly record struct OrganizationId(Guid Value)
{
    public static OrganizationId New() => new(Guid.CreateVersion7());

    public static OrganizationId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Organization id cannot be empty.", nameof(value));

        return new OrganizationId(value);
    }
}
