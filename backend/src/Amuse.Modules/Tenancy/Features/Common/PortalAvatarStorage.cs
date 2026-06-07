using Amuse.Domain.Identity;

namespace Amuse.Modules.Tenancy.Features.Common;

internal static class PortalAvatarStorage
{
    public const int MaxObjectKeyLength = 500;

    public static string BusinessPrefix(AccountId accountId) =>
        $"profiles/business/{accountId.Value}/";

    public static bool IsValidBusinessKey(string key, AccountId accountId) =>
        !string.IsNullOrWhiteSpace(key)
        && key.Length <= MaxObjectKeyLength
        && key.StartsWith(BusinessPrefix(accountId), StringComparison.Ordinal);
}
