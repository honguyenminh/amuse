using Microsoft.AspNetCore.Authorization;

namespace Amuse.Modules.Common.Authorization;

public enum OrgClaimMatchMode
{
    Any,
    All,
}

public sealed class OrgClaimRequirement : IAuthorizationRequirement
{
    public OrgClaimRequirement(IReadOnlyList<string> requiredClaims, OrgClaimMatchMode matchMode)
    {
        RequiredClaims = requiredClaims;
        MatchMode = matchMode;
    }

    public IReadOnlyList<string> RequiredClaims { get; }
    public OrgClaimMatchMode MatchMode { get; }
}
