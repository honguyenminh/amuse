using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Common.Persistence;

public class ModuleDbContextBase : DbContext
{
    public ModuleDbContextBase(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
