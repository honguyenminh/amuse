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

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<OrganizationInvite> OrganizationInvites => Set<OrganizationInvite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("tenancy");

        modelBuilder.HasPostgresEnum<OrganizationClass>(schema: "tenancy", name: "org_class");
        modelBuilder.HasPostgresEnum<OrganizationLifecycleStatus>(
            schema: "tenancy",
            name: "organization_lifecycle_status");
        modelBuilder.HasPostgresEnum<OrganizationOnboardingStatus>(
            schema: "tenancy",
            name: "organization_onboarding_status");
        modelBuilder.HasPostgresEnum<OrganizationTrustTier>(
            schema: "tenancy",
            name: "organization_trust_tier");

        modelBuilder.ApplyConfigurationsFromNamespace(
            typeof(TenancyDbContext),
            "Amuse.Modules.Tenancy.Persistence.Configurations");
    }
}
