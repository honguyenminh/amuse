namespace Amuse.Modules.Common.Authorization;

public static class OrgPolicies
{
    public const string ReadMembership = "OrgReadMembership";
    public const string ManageMembership = "OrgManageMembership";
    public const string ManageMemberPermissions = "OrgManageMemberPermissions";
    public const string ReadOrg = "OrgReadOrg";
    public const string ManageOrg = "OrgManageOrg";
    public const string ReadCatalog = "OrgReadCatalog";
    public const string WriteDraftCatalog = "OrgWriteDraftCatalog";
    public const string UploadCatalog = "OrgUploadCatalog";
    public const string PublishCatalog = "OrgPublishCatalog";
}
