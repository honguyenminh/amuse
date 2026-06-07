using Amuse.Domain.Tenancy;
using Amuse.Modules.Common.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Common.Persistence;

public static class TenantQueryFilter
{
    public static void ApplyOrganizationScope<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        IOrgScopeAccessor orgScopeAccessor,
        Func<TEntity, OrganizationId> organizationIdSelector)
        where TEntity : class
    {
        builder.HasQueryFilter(entity =>
            orgScopeAccessor.CurrentOrganizationId == null
            || organizationIdSelector(entity) == orgScopeAccessor.CurrentOrganizationId);
    }

    public static void ApplyManagingOrganizationScope<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        IOrgScopeAccessor orgScopeAccessor,
        Func<TEntity, OrganizationId?> managingOrganizationIdSelector)
        where TEntity : class
    {
        builder.HasQueryFilter(entity =>
            orgScopeAccessor.CurrentOrganizationId == null
            || managingOrganizationIdSelector(entity) == orgScopeAccessor.CurrentOrganizationId);
    }
}
