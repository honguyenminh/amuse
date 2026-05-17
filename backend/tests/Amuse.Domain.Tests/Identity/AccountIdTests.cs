using Amuse.Domain.Identity;

namespace Amuse.Domain.Tests.Identity;

public sealed class AccountIdTests
{
    [Fact]
    public void From_rejects_empty_guid()
    {
        Assert.Throws<ArgumentException>(() => AccountId.From(Guid.Empty));
    }

    [Fact]
    public void New_returns_version7_guid()
    {
        var id = AccountId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }
}
