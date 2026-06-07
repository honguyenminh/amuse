using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Common.Persistence;

public abstract class ModuleDbContextBase : DbContext
{
    protected ModuleDbContextBase(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
