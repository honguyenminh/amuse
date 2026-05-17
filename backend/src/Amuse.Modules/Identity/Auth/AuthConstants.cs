namespace Amuse.Modules.Identity.Auth;

public static class AuthConstants
{
    public const string LocalIdpIssuer = "local";

    public const string RefreshCookieName = "amuse_refresh";
    public const string ClientTypeHeader = "X-Amuse-Client";
    public const string WebClient = "web";
    public const string MobileClient = "mobile";
}
