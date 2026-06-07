using Amuse.Modules.Ingestion.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Ingestion.Tests;

public sealed class IngestionDbContextTests
{
    [Fact]
    public void Uses_ingestion_schema()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<IngestionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new IngestionDbContext(options);
        var entity = db.Model.GetEntityTypes().FirstOrDefault();
        Assert.Equal("ingestion", db.Model.GetDefaultSchema());
    }
}
