using Amuse.Domain.Identity;

namespace Amuse.Domain.Tests.Identity;

public sealed class PersonaContextTests
{
    [Fact]
    public void ForOrg_requires_non_empty_id()
    {
        Assert.Throws<ArgumentException>(() => PersonaContext.ForOrg(Guid.Empty));
    }

    [Fact]
    public void ForPlatform_has_no_scope_ids()
    {
        var ctx = PersonaContext.ForPlatform();
        Assert.Equal(PersonaContextType.Platform, ctx.Type);
        Assert.Null(ctx.OrgId);
        Assert.Null(ctx.ListenerId);
    }
}
