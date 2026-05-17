using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Persistence;

public sealed class TenancyDbContext : ModuleDbContextBase
{
    public TenancyDbContext(DbContextOptions<TenancyDbContext> options)
        : base(options)
    {
    }

    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("tenancy");
        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(TenancyDbContext),
            "Amuse.Modules.Tenancy.Persistence.Configurations");
    }
}
