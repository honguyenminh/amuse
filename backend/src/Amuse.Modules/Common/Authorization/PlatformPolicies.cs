namespace Amuse.Modules.Common.Authorization;

public static class PlatformPolicies
{
    public const string RequireOrganizationReview = "RequireOrganizationReview";
    public const string RequireOrganizationManage = "RequireOrganizationManage";
    public const string RequireAccountingRead = "RequireAccountingRead";
    public const string RequireAccountingManage = "RequireAccountingManage";
    public const string RequirePayoutManage = "RequirePayoutManage";
    public const string RequirePurchaseManage = "RequirePurchaseManage";
}
