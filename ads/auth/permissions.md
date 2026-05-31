# Organization and platform permissions catalog

Canonical claim strings for Amuse B2B org members and platform operators. See [auth-index.md](auth-index.md) for persona and preset-role rules.

## Claim format

```
{action}:{scope}:{target}
```

| Segment | Description |
|---------|-------------|
| `action` | Verb (`read`, `manage`, `upload`, `write_draft`, `publish_public`, `review`, …) |
| `scope` | Product area (`org`, `membership`, `catalog`, `payout`, `platform`) |
| `target` | `all` (entire scope) or, for catalog only, `{resourceKind}:{resourceId}` |

Examples:

- `read:catalog:all` — read any catalog resource in the org
- `read:catalog:artist:0194a7b2-c3d4-7890-abcd-ef1234567890` — read one artist (Version7 GUID)
- `review:platform:organizations` — review backing-org applications

## Matching rule

A required claim `R` is satisfied when the JWT `claims[]` contains:

1. `R` exactly, or
2. `{action}:{scope}:all` where `R` shares the same `action` and `scope`.

There is **no** implicit implication (e.g. `manage` does not grant `read`). Presets assign both where needed.

## Organization member claims (assignable)

| Claim | Meaning |
|-------|---------|
| `read:org:all` | View organization profile and settings |
| `manage:org:all` | Update organization settings; transfer ownership |
| `read:membership:all` | List members and pending invites |
| `manage:membership:all` | Invite, update, remove members |
| `manage:member_permissions:all` | Adjust member preset roles and fine-grained claims |
| `read:catalog:all` | View catalog |
| `upload:catalog:all` | Upload masters (when org capability allows) |
| `write_draft:catalog:all` | Create/edit drafts |
| `publish_public:catalog:all` | Publish to public catalog (approved backing orgs) |
| `read:payout:all` | View payout statements |

Per-resource catalog claims use kinds: `artist`, `release`, `track`, `release_group`.

## Capability-derived claims (mint time only)

Merged into the org persona JWT from `Organization.EvaluateCapabilities()`; not stored on `organization_member`:

- Same strings as above where the org lifecycle/onboarding allows the capability.

## Preset labels (UI snapshots)

| Label | Claims copied at assign time |
|-------|------------------------------|
| `admin` | Full owner/admin set (`read/manage` org + membership + permissions, catalog read/upload/draft) |
| `member_manager` | `read:org:all`, `read/manage:membership:all` |
| `catalog_editor` | `read:org:all`, `read/upload/write_draft:catalog:all` |
| `viewer` | `read:org:all`, `read:membership:all`, `read:catalog:all` |

## Platform operator claims

**Implementation rule:** All platform authorization and UI gating must use `PlatformClaims` in `Amuse.Domain.Platform` (backend) and `lib/auth/platformClaims.ts` (business frontend). Do **not** check only `manage:platform:organizations` — `platform:root` implies full manage + review.

| Claim | Meaning |
|-------|---------|
| `review:platform:organizations` | List/approve/reject backing organization applications; backing orgs created by this operator are **approved immediately** |
| `manage:platform:organizations` | Force-transfer organization ownership; recover soft-deleted organizations; **assume any organization persona** with owner-admin claims at token mint; backing orgs created by this operator are **approved immediately** |
| `manage:platform:all` | Same effective access as manage organizations (scope-wide) |
| `platform:root` | Break-glass full platform access (operator id `1` only). At token mint, expanded to include manage + review claims. Implies everything in this table. |

Legacy strings (`org:read`, `platform:organizations:review`, …) are migrated in the database and accepted only when normalizing stored rows.
