using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amuse.Modules.Common.Persistence;

/// <summary>
/// Reusable value converter + value comparer for <see cref="IReadOnlyList{T}"/> of strings
/// persisted as a Postgres <c>jsonb</c> column. The comparer is required by EF Core
/// (warning 10620) so that element-level mutations are detected by the change tracker.
/// </summary>
internal static class JsonStringListConverter
{
    public static ValueConverter<IReadOnlyList<string>, string> Converter { get; } =
        new(
            list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                    ?? new List<string>());

    public static ValueComparer<IReadOnlyList<string>> Comparer { get; } =
        new(
            (a, b) => a == null
                ? b == null
                : b != null && a.SequenceEqual(b, StringComparer.Ordinal),
            list => list == null
                ? 0
                : list.Aggregate(
                    0,
                    (hash, item) => HashCode.Combine(hash, StringComparer.Ordinal.GetHashCode(item))),
            list => list == null ? new List<string>() : list.ToArray());
}
