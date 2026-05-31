namespace Amuse.Modules.Tenancy.Features.ListMembers;

public sealed record ListMembersQuery(
    string? Search,
    string SortBy,
    string SortDirection,
    int Page,
    int PageSize)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public static ListMembersQuery From(
        string? search,
        string? sortBy,
        string? sortDirection,
        int? page,
        int? pageSize)
    {
        var normalizedPage = page is null or < 1 ? 1 : page.Value;
        var normalizedSize = pageSize switch
        {
            null or < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value,
        };

        var normalizedSort = (sortBy ?? "email").Trim().ToLowerInvariant();
        if (normalizedSort is not ("email" or "preset" or "lastlogin" or "lastactive" or "joined"))
            normalizedSort = "email";

        var normalizedDirection = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : "asc";

        return new ListMembersQuery(
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            normalizedSort,
            normalizedDirection,
            normalizedPage,
            normalizedSize);
    }
}
