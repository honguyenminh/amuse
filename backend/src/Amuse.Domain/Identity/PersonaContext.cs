namespace Amuse.Domain.Identity;

public sealed class PersonaContext
{
    public PersonaContextType Type { get; }
    public Guid? OrgId { get; }
    public Guid? ListenerId { get; }

    private PersonaContext(PersonaContextType type, Guid? orgId, Guid? listenerId)
    {
        Type = type;
        OrgId = orgId;
        ListenerId = listenerId;
    }

    public static PersonaContext ForOrg(Guid orgId)
    {
        if (orgId == Guid.Empty)
            throw new ArgumentException("Organization id cannot be empty.", nameof(orgId));

        return new PersonaContext(PersonaContextType.Org, orgId, null);
    }

    public static PersonaContext ForListener(Guid listenerId)
    {
        if (listenerId == Guid.Empty)
            throw new ArgumentException("Listener id cannot be empty.", nameof(listenerId));

        return new PersonaContext(PersonaContextType.Listener, null, listenerId);
    }

    public static PersonaContext ForPlatform() => new(PersonaContextType.Platform, null, null);
}
