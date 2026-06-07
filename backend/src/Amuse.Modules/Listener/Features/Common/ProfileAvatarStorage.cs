using Amuse.Domain.Identity;

namespace Amuse.Modules.Listener.Features.Common;

internal static class ProfileAvatarStorage
{
    public const int MaxObjectKeyLength = 500;

    public static string ListenerPrefix(AccountId accountId) =>
        $"profiles/listener/{accountId.Value}/";

    public static bool IsValidListenerKey(string key, AccountId accountId) =>
        IsValidKey(key, ListenerPrefix(accountId));

    private static bool IsValidKey(string key, string prefix) =>
        !string.IsNullOrWhiteSpace(key)
        && key.Length <= MaxObjectKeyLength
        && key.StartsWith(prefix, StringComparison.Ordinal);
}
