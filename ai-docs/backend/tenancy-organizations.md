# Tenancy: organizations and business portal

## Model

- **`Organization`** aggregate in `Amuse.Domain/Tenancy` with:
  - `org_class`: `indieGroup` \| `backingOrg`
  - `lifecycle_status`: `draft` \| `active` \| `suspended` \| `closed`
  - `onboarding_status`: `notRequired` \| `pendingReview` \| `approved` \| `rejected`
  - `trust_tier`: stub for DA1 (`unverified` default; `platformVerified` on backing approval)
- **`OrganizationMember`**: claims-based ACL; owner seeded with `OrgClaimPresets.OwnerAdmin`.
- **Capabilities** are derived via `Organization.EvaluateCapabilities()` and merged into JWT `claims[]` at org persona mint time (not stored on the org row).

## Onboarding paths

| Class | On create | Portal login | Publish public / payout |
|-------|-----------|--------------|-------------------------|
| Indie group | `active` + `notRequired` | Yes | No (upload/draft only) |
| Backing org | `active` + `pendingReview` | Yes (limited claims) | After platform `approve` |
| Backing org approved | `approved` + `platformVerified` | Yes | Yes (`catalog:publish_public`, `payout:read`) |

## HTTP API (`/api/v1/tenancy`)

| Method | Path | Auth |
|--------|------|------|
| POST | `/organizations` | Bearer (account) |
| GET | `/organizations` | Bearer (account) |
| GET | `/organizations/{id}` | Bearer (account, member) |

## Platform approval (`/api/v1/platform`)

Requires `ctx=platform` and claim `platform:organizations:review` or `platform:root`.

| Method | Path |
|--------|------|
| GET | `/organizations/applications?status=pendingReview` — backing org queue with **owner contact** (email, IdP, account status) for off-platform review |

Owner contact is resolved in **Tenancy** (`IOrganizationCreatorContactLookup` on `ListPendingBackingApplicationsAsync`), not Identity auth APIs. Identity only supplies the adapter that reads `ApplicationUser` / `Account` rows.
| POST | `/organizations/{id}/approve` |
| POST | `/organizations/{id}/reject` |

Audit actions: `organization_approved`, `organization_rejected`.

## Persistence

- Schema: `tenancy.organization`, `tenancy.organization_member`
- Postgres enums registered in `TenancyDbContext` + `MapEnum` in `TenancyDbContextOptions` (required for Npgsql).
- Migration: `AddOrganization`

## Errors (`TenancyErrors`)

| Code | HTTP |
|------|------|
| `tenancy.organization_not_found` | 404 |
| `tenancy.not_organization_member` | 404 |
| `tenancy.invalid_display_name` | 400 |
| `tenancy.invalid_onboarding_transition` | 400 |
| `tenancy.invalid_rejection_reason` | 400 |

## Business portal UI

Routes (business Next.js app, **sign-in required**; no account signup page):

| Route | Purpose |
|-------|---------|
| `/login` | Password sign-in |
| `/signup` | Register account (email confirmation required) |
| `/confirm-email` | Complete registration from email link |
| `/create-organization` | Create `indieGroup` or `backingOrg` (`?returnTo=` supported) |
| `/select-persona` | Workspace picker; footer link to create-organization |
| `/settings` | **Add organization** when active persona is org (hidden in platform persona) |

After create: reload personas → switch to new org → navigate to `returnTo` (default `/dashboard`). Backing orgs with `onboardingStatus: pendingReview` show `OrganizationStatusBanner` in `PortalShell`.

**Policy:** organization creation is **optional** — users with zero org/platform access still see the existing bootstrap error; there is no forced redirect to create-organization.

## Verification

```bash
# After migrate-all.sh and API running:
# 1. Login (platform or listener), 2. POST /tenancy/organizations, 3. refresh with org context
dotnet test tests/Amuse.Api.IntegrationTests --filter TenancyOrganizationFlowTests
dotnet test tests/Amuse.Domain.Tests --filter OrganizationCapabilities
```
