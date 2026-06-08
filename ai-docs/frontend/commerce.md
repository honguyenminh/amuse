# Commerce & finance (frontend)

Source of truth: `ads/finance/buying.md`, `ads/finance/payment.md`.

## API clients

| App | Client | Types |
| --- | ------ | ----- |
| Consumer | `frontend/consumer/src/lib/api/financeClient.ts` | `financeTypes.ts` |
| Business | `frontend/business/src/lib/api/financeClient.ts` | shared billing DTOs |

Use `pnpm` for all frontend commands.

## Consumer app

| Surface | Path | Behavior |
| ------- | ---- | -------- |
| Release buy / free | Release detail page | PWYW floor pricing; free acquire when floor = 0; paid → checkout session → Stripe redirect |
| My purchases | `/library/purchases` | Lists purchases; `?checkout=success` polls after Stripe return |
| Track download | `TrackDownloadButton` | Owner-only download via billing download API |
| Stream preview cap | `selectRendition` | Non-owners capped at ≤128 kbps via `stream-info.isOwner` |

## Business portal

| Surface | Path | Claim gate |
| ------- | ---- | ---------- |
| Finance nav | Sidebar | `read:payout:all` |
| Payout setup (Gate B) | `/finance/payout-setup` | `manage:payout:profile:all` |
| Balance | `/finance/balance` | `read:payout:all` |
| Withdraw | `/finance/withdraw` | `manage:payout:withdraw:all` |
| Release pricing | Release editor `ReleasePricingPanel` | `manage:catalog:pricing:all` |

### Payout setup wizard

- Collects legal entity, address, tax ID, bank details, document keys
- `payoutRail`: `manual_bank` (ops review queue) or `stripe_global` (Stripe Account Link)
- For `stripe_global`: call `POST /api/v1/billing/payout-profile/stripe-account-link` and redirect seller to returned URL
- Material changes after verification move profile to `under_review` and block withdrawals

### Withdraw flow

- Shows per-currency `pending`, `available`, `in_payout`, `receivable`
- USD equivalent from ECB rates (informational)
- Cooldown end displayed when active
- `stripe_global` + verified + under threshold: auto-processed on backend
- `manual_bank`: always enters platform ops queue

## Platform portal

| Surface | Path | Claim |
| ------- | ---- | ----- |
| Accounting | `/platform/accounting` | `read:platform:accounting:all` |
| Purchases / refunds | `/platform/purchases` | `manage:platform:purchases:all` |
| Payout profiles | `/platform/payout-profiles` | `manage:platform:payouts:all` |
| Withdrawals | `/platform/withdrawals` | `manage:platform:payouts:all` |

Use `platformClaims.ts` helpers — never raw-check only `manage:platform:organizations`; `platform:root` expands via `PlatformClaims`.

## Money display

- `frontend/business/src/lib/finance/formatMoney.ts` — minor units + ISO currency
- `frontend/consumer/src/lib/finance/pricingDisplay.ts` — PWYW floor/ceiling display

## Local dev

- Stripe checkout redirect URLs: `Billing:Checkout` in API appsettings (consumer `localhost:3000`)
- Stripe Account Link return URLs: `Billing:GlobalPayout` (business `localhost:3001`)
- Without Stripe keys, checkout/payout endpoints return `billing.checkout.not_configured` / `billing.payout.not_configured`
