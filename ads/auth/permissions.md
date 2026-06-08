# Organization and platform permissions catalog

Canonical claim strings for Amuse B2B org members and platform operators. See [auth-index.md](auth-index.md) for persona and preset-role rules.

## Claim format

```
{action}:{scope}:{target}
```

| Segment | Description |
|---------|-------------|
| `action` | Verb (`read`, `manage`, `upload`, `write_draft`, `publish_public`, `review`, …) |
| `scope` | Product area (`org`, `membership`, `catalog`, `payout`, `platform`, `accounting`) |
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
| `manage:catalog:pricing:all` | Set track/release price floor and ceiling (pay what you want); see [buying.md](../finance/buying.md) |
| `read:payout:all` | View payout statements |
| `manage:purchase:refund:all` | Initiate refund on purchases where org is a payee ([payment.md §15](../finance/payment.md#15-pricing-authority-and-refund-claims-locked)) |

Per-resource catalog claims use kinds: `artist`, `release`, `track`, `release_group`.

## Capability-derived claims (mint time only)

Merged into the org persona JWT from `Organization.EvaluateCapabilities()`; not stored on `organization_member`:

- `read:org:all`, `read:membership:all` when the org lifecycle allows those capabilities
- `upload:catalog:all`, `write_draft:catalog:all`, `publish_public:catalog:all` when the org allows the matching catalog write capability

**Not** capability-derived (must be assigned on the member or copied from a preset): `read:catalog:all`, per-resource catalog read claims, all catalog write/manage claims (`upload`, `write_draft`, `publish_public`, pricing), and all payout/purchase claims including `read:payout:all`.

**Per-resource read inheritance:** `read:catalog:artist:{id}` grants read access to that artist and to release groups, releases, and tracks under that artist. `read:catalog:release_group:{id}` also grants read on releases in that group. `read:catalog:release:{id}` also grants read on tracks in that release.

## Preset labels (UI snapshots)

| Label | Claims copied at assign time |
|-------|------------------------------|
| `admin` | Full owner/admin set (`read/manage` org + membership + permissions, catalog read/upload/draft/**pricing**, **purchase refund**) |
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
| `read:platform:accounting:all` | View tax invoices, VAT liability summaries, accounting exports ([payment.md §10](../finance/payment.md#10-tax-and-invoicing-locked)) |
| `manage:platform:accounting:all` | Issue credit notes, accounting adjustments, period-close helpers (with audit) |
| `manage:platform:purchases:all` | Refund any purchase; choose refund fee bearer ([payment.md §15](../finance/payment.md#15-pricing-authority-and-refund-claims-locked)) |
| `manage:platform:payouts:all` | Approve/reject seller withdrawals above auto-approve threshold ([payment.md §12](../finance/payment.md#12-withdrawals-locked)) |
| `platform:root` | Break-glass full platform access (operator id `1` only). At token mint, expanded to include manage + review + accounting + purchases + payouts claims. Implies everything in this table. |

Legacy strings (`org:read`, `platform:organizations:review`, …) are migrated in the database and accepted only when normalizing stored rows.
