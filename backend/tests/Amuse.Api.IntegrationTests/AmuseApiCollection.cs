namespace Amuse.Api.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class AmuseApiCollection : ICollectionFixture<AmuseApiFixture>
{
    public const string Name = "AmuseApi";
}
