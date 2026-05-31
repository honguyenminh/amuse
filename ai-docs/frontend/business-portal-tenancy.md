# Business portal: tenancy and platform ops

Next.js app under `frontend/business/`. Auth and API clients live in `src/lib/`.

## Org workspace routes

| Route | Purpose |
|-------|---------|
| `/members` | Member list, invites, role dialog, transfer/remove (claim-gated) |
| `/members/invites` | Pending invites with revoke confirm |
| `/settings` | Add organization; **membership** (leave); **delete organization** (owner) |
| `/accept-invite?token=` | Invite preview, accept/decline |
| `/platform/applications` | Backing org review; **closed org recover**; **force-transfer** (manage claim) |

`/settings/members` redirects to `/members`.

## Settings: leave vs delete

- **Leave** (`POST .../membership/leave`): non-owners only; soft-removes own membership.
- **Delete organization** (`DELETE .../organizations/{id}`): owner + `manage:org:all`; sets org `lifecycle_status = closed`.

After leave or delete, personas reload and the user is sent to **Select workspace** (`/select-persona?switch=1`).

## Closed organization handling (client)

When an org-scoped API returns `404` with `tenancy.organization_not_found` (org soft-deleted or unavailable):

1. `authFetch` notifies `orgSessionEvents`.
2. `AuthProvider` reloads personas, switches to another workspace, and sets `orgUnavailableNotice`.
3. `PortalGate` shows an amber banner; user can dismiss.

Org-tenant routes are blocked server-side by `ActiveOrganizationMiddleware` for closed orgs.

## Platform operations UI

Requires platform persona.

| Section | Claim | API |
|---------|-------|-----|
| Pending applications | `review:platform:organizations` | `GET /platform/organizations/applications?status=pendingReview` |
| Closed organizations | `manage:platform:organizations` | `GET /platform/organizations/closed`, `POST .../recover` |
| Force transfer ownership | `manage:platform:organizations` | `POST .../force-transfer-ownership` |

**Platform claim gating:** Use `lib/auth/platformClaims.ts` (`canManagePlatformOrganizations`, `canReviewPlatformOrganizations`) — not raw `hasClaim(..., "manage:platform:organizations")`. `platform:root` implies full manage + review (see `PlatformClaims.ExpandEffectiveClaims` on the backend).

## Verification (manual)

1. Owner: Settings → Delete organization → confirm → redirected to workspace picker; org absent from list.
2. Open old org URL with stale token → members/organization 404; banner + workspace switch.
3. Platform: Applications → Closed organizations → Recover → org visible again in tenant list.

```bash
dotnet test tests/Amuse.Api.IntegrationTests --filter TenancyOrganizationFlowTests
```
