# Buying tracks and releases (DA1)

Source of truth for consumer purchase entitlements, pricing rules, and playback/download rights. Payout and seller ledger rules live in [payment.md](payment.md).

## DA1 scope: no subscriptions

**Subscriptions are out of scope for DA1** (no Free vs Premium product tier, no recurring billing, no subscription pool royalties in DA1).

DA1 monetization for listeners is **one-time purchase only**. Account-level streaming quality is:

| Account | Catalog access | Max stream quality | Download |
|---------|----------------|-------------------|----------|
| **Unpaid** (default) | Public published catalog | **~128 kbps** (lossy cap; “1XX kbps max”) | No |
| **Owner** (bought track or release) | Owned items in **My purchases** + same catalog rules for non-owned | **Full ladder** (200+ kbps and **lossless** where transcoded) | Yes, **unlimited re-download** |

Implementation note: remove or stub Premium/subscription entitlement checks in DA1 playback paths; gate quality on **purchase ownership** instead.

---

## Locked product decisions

| # | Decision |
|---|----------|
| 1 | **My purchases** — bought track or release stays in the buyer’s library **forever** (account-bound). |
| 2 | **Stream quality** — owners stream purchased content at **max available quality**; non-owners on that content follow unpaid rules if the track is otherwise streamable at 128 kbps. |
| 3 | **Download** — owners may **download** what they bought (**track** or **release** scope), **unlimited** re-downloads. |
| 4 | **One purchase per account per track** — duplicate buy blocked; **no gifts** in DA1. |
| 5 | **Release pricing** — release **floor ≤ Σ track floors**; if release **ceiling** set, **ceiling ≤ Σ track ceilings** (when defined). See **Pricing**. |
| 6 | **Release entitlement** — buyer gets release-level ownership when they **buy the release** **or** **buy every track** on that release (see below). |
| 7 | **Pay what you want (PWYW)** — **floor = 0**; optional **ceiling**; buyer chooses amount in `[floor, ceiling]`. **Zero → no payment/payout** (entitlement only). See **Pricing**. |

---

## What can be bought

| Unit | DA1 | Entitlement granted |
|------|-----|---------------------|
| **Track** | Yes | Single-track ownership: max-quality stream + download for that track |
| **Release** | Yes | All tracks on that release: max-quality stream + download per track; release appears as one owned unit in My purchases |

Album/release-group level purchase (bundle above a single `Release`) is **DA2** unless explicitly added later.

---

## Release entitlement (buy release OR complete the set)

Two paths to **release ownership**:

1. **Direct** — checkout for the **release** line item (one payment, release price).
2. **Incremental** — buyer purchases **each track** on the release individually; when the **last missing track** is paid for, the platform grants **release entitlement** (equivalent to buying the release).

Rules:

- Incremental path: when remaining tracks would cost more than a **release checkout** at the buyer’s chosen release amount, offer **release checkout** instead (avoid punishing track-by-track buyers).
- **My purchases** shows release row when release entitlement is active (direct or completed set).
- Refunding **any** track on a release-backed entitlement revokes **release-level** ownership if no longer complete; partial refund policy follows [payment.md](payment.md) (operator/seller initiated only).

---

## Pricing (pay what you want)

Bandcamp-style **name your price**: each sellable track/release has a **floor** and optional **ceiling** (minor units + currency).

| Field | Meaning |
|-------|---------|
| `price_floor_minor` | Minimum buyer may pay; **0 = free allowed** |
| `price_ceiling_minor` | Optional max; omit or set **equal to floor** for ordinary fixed price |

**Buyer checkout amount** must satisfy: `floor ≤ amount ≤ ceiling` (if ceiling set; else `amount ≥ floor` only).

### **Modes**

| Configuration | UX |
|---------------|-----|
| `floor = 0`, no ceiling | Free or any tip (open ceiling) |
| `floor = 0`, `ceiling = floor` | Free only |
| `floor > 0`, `ceiling = floor` | Fixed price (usual storefront) |
| `floor > 0`, `ceiling > floor` | PWYW between min and max |
| `floor > 0`, no ceiling | Minimum price; buyer may pay more (open ceiling above floor) |

### **Zero amount — no payment rail**

When buyer chooses **`amount = 0`** (only possible if `floor = 0`):

- **Skip** PSP checkout, fees, ledger seller credits, tax invoice, and 3-day hold.
- Still record a **`Purchase`** (or `Acquisition`) with `payment_status = free`, `amount = 0`, grant **full owner entitlement** (My purchases, max quality, download).
- Enforce **one per account per track** same as paid.

### **Paid amount > 0**

Normal flow: [payment.md](payment.md) waterfall, tax invoice, royalty split on **actual amount paid** (snapshot on purchase).

Release allocation by track ratio uses **each track’s share of the release checkout amount** — for PWYW release checkout, split `gross` across tracks by **track floor weights** (or equal split if all floors zero — pick equal split at implementation and document in Billing).

### **Release vs track bounds**

- **`release.floor ≤ Σ track.floor`** on that release.
- If **`release.ceiling`** is set and all tracks have ceilings: **`release.ceiling ≤ Σ track.ceiling`**.
- Seller sets bounds at publish; edits apply to **future** checkouts only.

### **Who may set prices**

Claim **`manage:catalog:pricing:all`** — **owner admin** preset by default; may assign to catalog editors. Set when publishing for sale. See [payment.md §15](payment.md#15-pricing-authority-and-refund-claims-locked).

---

## Entitlement checks (Playback / consumer)

Priority for a given track:

1. **Owner** (track or parent release entitlement) → allow stream at full rendition ladder + allow download API.
2. **Else** if track is publicly streamable unpaid → allow stream capped at **128 kbps** (no lossless/high).
3. **Else** → deny stream (not published / not available).

Download endpoints require **owner** entitlement; never available to unpaid listeners.

Edge JWT / manifest issuance must encode max bitrate or rendition allow-list consistent with the above (fail-closed at CDN edge per existing playback spec).

---

## Purchase aggregate (Billing)

Minimum fields beyond payment state (see [payment.md](payment.md)):

- `account_id`, `organization_id` (seller)
- `purchased_unit`: `track` \| `release`
- `track_id` or `release_id`
- `price_snapshot_minor`, `currency`
- `payment_status`, `entitlement_status`
- `purchased_at` (timestamptz)

Unique constraint: `(account_id, track_id)` where `purchased_unit = track` and entitlement active.

Release entitlement: separate row or derived view keyed by `(account_id, release_id)`.

---

## Relationship to seller payouts

**Paid purchases** (`amount > 0`): ledger waterfall per [payment.md](payment.md). **Free acquisitions** (`amount = 0`): **no** seller ledger, PSP, or invoice; entitlement only.

---

## Decision checklist cross-ref

All 12 commerce/payout decisions are locked. See [payment.md](payment.md) checklist and sections §2–§16.

| Checklist # | Topic | Doc |
|-------------|--------|-----|
| 2–3, 7, 11 | Buying, PWYW, entitlements | **this doc** |
| 1, 4–6, 8–10, 12 | MoR, fees, tax, payouts, FX, org lifecycle | [payment.md](payment.md) |
| — | Subscriptions | **Deferred** — not DA1 |
