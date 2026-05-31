# Tenancy: members, invites, and claims

## Claim format

Stored on `organization_member.claims` and invite rows as jsonb strings:

- `{action}:{scope}:{target}` with `target = all` or `{resourceKind}:{resourceId}` for catalog.
- Domain type: `OrgClaim` in `Amuse.Domain/Tenancy/OrgClaim.cs`.
- Matcher: exact claim or scope-wide `action:scope:all`.

Migration `AddOrganizationInvitesAndMigrateClaims` rewrites legacy `resource:action` strings in existing rows.

## Presets

`GET /api/v1/tenancy/claim-presets` (anonymous) returns `OrgClaimPresets.AllDefinitions` with `label`, `displayName`, `description`, `icon`, and `claims`.

## HTTP API (`/api/v1/tenancy`)

| Method | Path | Auth | Required claim |
|--------|------|------|----------------|
| GET | `/claim-presets` | Anonymous | — |
| GET | `/organizations/{orgId}/members` | Org persona + route `org_id` | `read:membership:all` |

Query params: `search`, `sortBy` (`email` \| `preset` \| `lastlogin` \| `lastactive` \| `joined`), `sortDirection` (`asc` \| `desc`), `page`, `pageSize` (max 100). Response includes `joinedAt`, `lastLoginAt`, `lastActiveAt` (from refresh sessions / accepted invites).
| POST | `/organizations/{orgId}/members/invites` | Org persona | `manage:membership:all` |
| GET | `/organizations/{orgId}/members/invites` | Org persona | `read:membership:all` |
| DELETE | `/organizations/{orgId}/members/invites/{inviteId}` | Org persona | `manage:membership:all` |
| PATCH | `/organizations/{orgId}/members/{memberId}` | Org persona | `manage:member_permissions:all` |
| DELETE | `/organizations/{orgId}/members/{memberId}` | Org persona | `manage:membership:all` |
| POST | `/organizations/{orgId}/membership/leave` | Org persona | active non-owner member (self) |
| POST | `/organizations/{orgId}/ownership/transfer` | Org persona | owner + `manage:org:all` |
| DELETE | `/organizations/{orgId}` | Org persona | owner + `manage:org:all` (soft-delete → `closed`) |
| GET | `/invites/{token}` | Anonymous | — |
| POST | `/invites/{token}/accept` | Bearer (account) | email match, pending invite |
| POST | `/invites/{token}/decline` | Bearer (account) | email match, pending invite (revokes invite) |

Platform: `POST /api/v1/platform/organizations/{id}/force-transfer-ownership` with `manage:platform:organizations` or `platform:root`. `POST /api/v1/platform/organizations/{id}/recover` restores a soft-deleted org (`closed` → `active`).

## Invite flow

1. Admin with org persona calls create invite → email via `ITenancyInviteEmailSender` (`Tenancy:BusinessPortalBaseUrl` + `/accept-invite?token=...`).
2. Invitee signs up or signs in (email must match).
3. `POST .../accept` creates `OrganizationMember`, marks invite accepted.
4. Client `POST /identity/refresh` with `context.type = org` → JWT includes new `claims[]`.

## Owner rules (domain)

- Owner cannot be removed, cannot leave (`tenancy.owner_cannot_leave_organization`), and cannot be demoted below admin-equivalent claims.
- Non-owners may leave via `POST .../membership/leave` (soft-remove, same as admin remove).
- Only owner may transfer ownership to another active member.
- Platform may force-transfer via `OrganizationMember.ForceOwnershipFrom`.

## Errors (`TenancyErrors`)

| Code | HTTP |
|------|------|
| `tenancy.member_not_found` | 404 |
| `tenancy.invite_not_found` | 404 |
| `tenancy.invite_expired` | 410 |
| `tenancy.invite_email_mismatch` | 403 |
| `tenancy.duplicate_member` | 409 |
| `tenancy.duplicate_pending_invite` | 409 |
| `tenancy.cannot_modify_owner` | 400 |
| `tenancy.cannot_demote_owner` | 400 |
| `tenancy.not_organization_owner` | 400 |
| `tenancy.invalid_claim` | 400 |
| `tenancy.claim_not_allowed_for_organization` | 400 |
| `tenancy.insufficient_claim` | 403 |

## Business portal UI

| Route | Purpose |
|-------|---------|
| `/members` | Sidebar tab: list members/invites, invite, role assignment dialog, transfer/remove (JWT claim gating) |
| `/accept-invite?token=` | Preview + login/signup + accept |

Role assignment uses **Assign role** dialog (preset cards with display name, description, icon, granted permissions). Fine-grained claims editor is stubbed until catalog resources exist. `manage:member_permissions:all` gates role changes; `manage:membership:all` gates invite/remove.

`/settings/members` redirects to `/members`.

## Verification

```bash
dotnet ef database update --project src/Amuse.Modules/Amuse.Modules.csproj --startup-project src/Amuse.Api/Amuse.Api.csproj --context TenancyDbContext
dotnet test tests/Amuse.Domain.Tests --filter OrgClaim
dotnet test tests/Amuse.Api.IntegrationTests --filter TenancyMemberFlowTests
```

After changing a member's claims, refresh the org persona token to see updated permissions (FR-007).
