# Commerce & Payout Implementation Plan

**Source of truth (product):** [buying.md](buying.md), [payment.md](payment.md), [permissions.md](../auth/permissions.md)

**Architecture plan (Cursor):** `.cursor/plans/commerce_implementation_plan_b2288cd6.plan.md` — do not edit; update this file for phase status only.

---

## Architecture — State pattern vs enum lifecycles

Use a **plain enum + guard checks** when the lifecycle is small (≤4 states), transitions are mostly linear, and side effects live outside the aggregate (handlers/services).

Use the **State pattern** (small state classes + aggregate context) when:

- Valid transitions form a **non-trivial graph** (skip steps, re-entry, parallel paths).
- **Entry/exit rules differ by state** (e.g. edits locked in review, material change re-opens verification).
- The same command must **behave differently** depending on current state beyond a single `if`.
- Transition logic is **scattered** across multiple aggregate methods or duplicated in handlers.

**Keep postgres enums for persistence.** Map `Status` / `VerificationStatus` columns to domain enums; domain code routes transitions through state objects. After EF materialization, lazily resolve state from the stored enum (`_state ??= States.From(Status)`).

### Billing candidates

| Aggregate | Enum(s) | Complexity | Recommendation |
| --------- | ------- | ---------- | -------------- |
| `WithdrawalRequest` | `WithdrawalStatus` | Medium — DA1 manual rail, skip-approve path, terminal fail/complete | **Done (exemplar)** — `Billing/Withdrawals/WithdrawalRequestState.cs` |
| `PayoutProfile` | `PayoutVerificationStatus` | High — submit/review/verify/reject + material-change re-review + edit locks | **Done** — `Billing/PayoutProfiles/PayoutProfileState.cs` |
| `Purchase` | `PaymentStatus` + `EntitlementStatus` | High — paid/refund/chargeback cross-cuts entitlement + refund metadata | Deferred — optional composite or split state objects per axis |
| `PaymentTransaction` | `PaymentStatus` | Low–medium — mostly mirrors purchase PSP leg | Deferred — may stay enum if kept thin |

### Conventions (Billing state refactor)

- State types are `internal` to the bounded context; public surface stays on the aggregate (`MarkApproved`, `Submit`, …).
- Invalid transitions return `Result.Failure(BillingErrors.*Invalid*Transition)` — no exceptions.
- Domain tests must cover **happy paths and rejected transitions** per command.
- Handlers/modules do not branch on raw status for transition rules — only the aggregate (via state) decides.

---

## Phase tracker

| Phase | Name                    | Exit criteria                                                         | Status      |
| ----- | ----------------------- | --------------------------------------------------------------------- | ----------- |
| 0     | Foundation              | Billing project wired; Money type; claims/tenancy fixed               | completed   |
| 1     | Sellable catalog        | PWYW fields + RoyaltySplit + publish validation + business pricing UI | completed   |
| 2     | Free purchases          | $0 acquire, My purchases API + consumer library                       | completed   |
| 3     | Playback & download     | 128 kbps cap, owner full quality, download endpoint + UI              | completed   |
| 4     | Paid checkout & ledger  | Stripe sandbox, waterfall, VAT invoice, 3-day hold job                | completed   |
| 5     | Discovery trust         | Unverified badge + lower search rank                                  | completed   |
| 6     | Payout profile Gate B   | KYC aggregate, ops review, business onboarding UI                     | completed   |
| 7     | Balance & withdrawals   | Balance read, withdrawal lifecycle, manual rail DA1                   | completed   |
| 8     | Platform finance ops    | Accounting screens, payout approval, refunds                          | completed   |
| 9     | Stripe payouts & FX     | Global Payouts rail, ECB import, auto-approve threshold               | completed   |
| 10    | Chargebacks & lifecycle | Account/card ban, receivable, org suspend/close ledger rules          | completed   |
| 11    | Hardening               | Integration/domain tests, ai-docs, compose/worker wiring; State pattern on remaining Billing lifecycles | completed   |

---

## Phase 0 notes

- Billing bounded context scaffolded under `backend/src/Amuse.Domain/Billing/` and `backend/src/Amuse.Modules/Billing/`.
- Indie active orgs receive `CanReadPayout` via `Organization.EvaluateCapabilities()`.
- Platform accounting/purchases/payouts claims added to `PlatformClaims` + business portal mirror.
- Identity `Account.Ban()` blocks login and refresh.

**Pain points for later phases:** Existing member JWT claim snapshots do not auto-update when presets change — re-assign preset or re-invite if needed. Billing schema grows per phase; keep migrations incremental.

## Phase 1 notes

- Catalog `Track` / `Release` gained `is_for_sale`, `price_floor_minor`, `price_ceiling_minor` (nullable = open ceiling), `price_currency`.
- `RoyaltySplit` aggregate in Catalog BC (`catalog.royalty_split`); default 100% listing org when no rows.
- Domain validation on publish: split sum 10000 bps, release floor ≤ Σ track floors, release ceiling ≤ Σ track ceilings when all track ceilings set.
- Pricing changes blocked when org lifecycle is `suspended` or `closed` (`ITenancyOrganizationReadModel.GetLifecycleStatusAsync`).
- API slices: `PATCH .../tracks/{id}/pricing`, `PATCH .../releases/{id}/pricing`, `PUT .../tracks/{id}/royalty-splits` — gated by `OrgPolicies.ManageCatalogPricing`.
- Business portal: release editor Sales & pricing panel (`ReleasePricingPanel`).
- Migration: `AddCatalogSellablePricing` on `CatalogDbContext`.

## Phase 2 notes

- `Purchase.AcquireFreeTrack` / `AcquireFreeRelease` — zero amount, `payment_status=free`, active entitlement, no PSP/ledger.
- Unique indexes on `(account_id, track_id)` and `(account_id, release_id)` filtered to **active** entitlements only (`UpdatePurchaseEntitlementIndexes` migration).
- Complete-the-set: acquiring the last sellable track on a release grants a release-level purchase via `ReleaseEntitlementCompletion`.
- `EntitlementQuery` + `IEntitlementReadModel`: `OwnsTrack` (track or parent release), `OwnsRelease` (direct or all sellable tracks owned).
- `ICatalogPurchaseReadModel` validates `is_for_sale` + `floor=0` for free path.
- Consumer API (listener persona): `POST /api/v1/billing/acquisitions/free`, `GET /api/v1/billing/purchases/me`, `GET /api/v1/billing/entitlements/ownership`.
- Consumer UI: `financeClient.ts`, release page PWYW + “Get for free”, `/library/purchases` tab, paid “Buy” stub disabled until Phase 4.
- Domain tests: `Amuse.Domain.Tests/Billing/`; handler tests: `Amuse.Modules.Billing.Tests/`.

## Phase 3 notes

- `GetTrackStreamInfoHandler` injects `IEntitlementReadModel`: owners get full rendition ladder + `isOwner: true`; non-owners on published tracks get renditions capped at ≤128 kbps (no FLAC); else `catalog.stream_playback_forbidden` (403).
- `GET /api/v1/billing/downloads/tracks/{trackId}` — listener persona, owner-only; presigned GET to `audio_master_key` via `IObjectStorage`.
- Consumer: `stream-info.isOwner` drives `selectRendition` preview cap; `TrackDownloadButton` on My purchases track rows and owned release track rows.
- Tests: `GetTrackStreamInfoHandlerTests` (rendition filter), `DownloadTrackHandlerTests` (403 non-owner).

## Phase 4 notes

- **Stripe checkout:** `ICheckoutProvider` + `StripeCheckoutProvider` (Stripe.net 52); `POST /api/v1/billing/checkout/sessions` creates pending `Purchase` + `PaymentTransaction`, validates PWYW amount against catalog floor/ceiling, blocks suspended/closed seller orgs.
- **Webhook:** `POST /api/v1/billing/webhooks/stripe` handles `checkout.session.completed` (entitlement + ledger + tax invoice) and `charge.refunded` (revoke entitlement, mark refunded). Stores `payment_method_fingerprint` and `psp_fee_minor` on `PaymentTransaction`.
- **Waterfall:** `PurchaseWaterfall` domain calculator — inclusive VAT extraction, platform fee on gross, PSP fee reduces seller pool; `JournalPoster.PostPurchase` credits `SellerPayablePending` with `available_at = paid_at + 3 days`.
- **Allocation:** `PurchaseAllocation` — release splits by track floor weights (equal if all zero), then per-track royalty splits; largest-remainder rounding; immutable `PurchaseAllocationSnapshot` rows.
- **Tax invoice:** sequential `AM-{year}-{seq}` via `TaxInvoiceNumber`; issued on paid purchase.
- **Hold job:** `PendingToAvailableWorker` in `Amuse.Worker.Scheduler` posts `HoldRelease` journals; skips suspended/closed orgs per payment.md §16.
- **Platform accounting:** `GET /api/v1/platform/accounting/invoices` gated by `read:platform:accounting:all` (`PlatformAccountingReadHandler`).
- **Consumer UI:** Buy buttons call checkout session → Stripe redirect; `/library/purchases?checkout=success` polls purchases.
- **Config:** `Billing:Stripe`, `Billing:Checkout`, `Billing:Hold`, `BillingScheduler` in appsettings.
- **Migration:** `AddPaymentTransactionCheckoutFields` (`checkout_session_id`, `psp_fee_minor`).
- **Tests:** `PurchaseWaterfallTests`, `PurchaseAllocationTests`, `JournalPosterTests`, `CheckoutPricingGuardTests`, `CreateCheckoutSessionHandlerTests`.

**Pain points for later phases:** PWYW UI uses floor amount only (no tip input). Stripe keys must be configured for live sandbox testing.

## Phase 6 notes

- **`PayoutProfile` aggregate** — full Gate B lifecycle (`not_started` → `submitted` → `under_review` → `verified` | `rejected`); material change after `verified` → `under_review` and blocks withdrawals.
- **Sensitive fields** — `tax_id` and bank account encrypted at rest via ASP.NET Data Protection (`ISensitiveFieldProtector`); API exposes `hasTaxId` + masked bank (`****last4`) only.
- **Org API** — `GET/PUT /api/v1/billing/payout-profile`, `POST .../submit` gated by `read:payout:all` / `manage:payout:profile:all`.
- **Platform API** — `GET /api/v1/platform/payout-profiles?status=under_review`, `POST .../{orgId}/approve|reject` gated by `manage:platform:payouts:all` (`PlatformPayoutManageHandler`).
- **Business UI** — `/finance/payout-setup` wizard; Finance nav item when `read:payout:all`; dashboard CTA when Gate B incomplete (shows even with Phase 7 balance stub at zero).
- **Platform UI** — `/platform/payout-profiles` review queue for operators with payout manage claim.
- **Migration** — `ExpandPayoutProfileGateB` adds address, encrypted tax/bank, documents jsonb, verified_by, rejection_reason.
- **Tests** — `PayoutProfileTests` (domain transitions), `PayoutProfileHandlerTests` (approve/reject/encrypt).

## Phase 7 notes

- **Balance API** — `GET /api/v1/billing/balance` returns per-currency `pending`, `available`, `in_payout`, `receivable`, optional USD equivalent (ECB stub), Gate B flags, cooldown end, receivable block (`read:payout:all`).
- **Statements API** — `GET /api/v1/billing/statements` paginates `PurchaseAllocationSnapshot` credit lines for the org.
- **Withdrawals** — `POST/GET /api/v1/billing/withdrawals` gated by `manage:payout:withdraw:all` / `read:payout:all`; DA1 manual rail always `pending_approval`; validates Gate B, 7-day cooldown, $10 USD eq. min (ECB stub/`fx_rate` table), available balance, no receivable; reserves `DR Available / CR InPayout` on request.
- **Platform ops** — `GET /api/v1/platform/withdrawals`, `POST .../approve|complete|fail` gated by `manage:platform:payouts:all`; complete posts `DR InPayout / CR PlatformCash`; fail reverses reserve to available.
- **Domain** — `WithdrawalRules`, `SellerLedgerBalance`, `FxRateConversion`, `JournalPoster.PostWithdrawal*` journals; `WithdrawalRequest` full lifecycle + `proof_object_key`.
- **Business UI** — `/finance/balance`, `/finance/withdraw`; platform `/platform/withdrawals` queue.
- **Migration** — `AddWithdrawalProofAndFailedAt`.
- **Tests** — `WithdrawalRulesTests`, `SellerLedgerBalanceTests`, `FxRateConversionTests`, `BalanceAndWithdrawalHandlerTests`.

## Phase 8 notes

- **Refund API** — `POST /api/v1/billing/purchases/{id}/refund` for platform (`manage:platform:purchases:all`, sets `refundFeeBearer`) or seller org (`manage:purchase:refund:all`, seller always bears fees). Stripe `RefundService` + idempotent `RefundCompletionService`.
- **Ledger** — `JournalPoster.PostRefund` mirrors purchase waterfall; `RefundClawback` debits pending → available → `RefundLiability` + `SellerReceivable` for shortfalls; platform/seller refund fee bearer rules per payment.md §2.
- **Credit notes** — `credit_note` table linked to `tax_invoice`; sequential `CN-{year}-{seq}` numbering.
- **Webhook** — `charge.refunded` delegates to `RefundCompletionService` (defaults seller fee bearer when refund initiated externally).
- **Platform API** — `GET /api/v1/platform/accounting/invoices`, `GET .../vat-summary`, `GET /api/v1/platform/purchases` (search/refund queue).
- **Business UI** — `/platform/accounting`, `/platform/purchases`; platform nav links accounting, purchases, payout-profiles, withdrawals; seller refund action on `/finance/balance` statement lines.
- **Migration** — `AddRefundAndCreditNotes` (purchase refund metadata + `credit_note` + `refund_initiator_role` enum).
- **Tests** — `RefundLedgerTests`, `RefundPurchaseHandlerTests` (fee bearer + clawback).

## Phase 9 notes

- **`IGlobalPayoutProvider`** — `StripeGlobalPayoutProvider` (Connect custom account, Account Link onboarding, Transfer outbound payments; sandbox-compatible — upgrade to V2 Outbound Payments when Stripe.net exposes `v2.money_management.outbound_payments`).
- **Account Link API** — `POST /api/v1/billing/payout-profile/stripe-account-link` creates recipient if needed and returns hosted onboarding URL.
- **Auto-approve** — `WithdrawalRules.ShouldAutoApproveStripeWithdrawal`: `stripe_global` + verified + USD equivalent ≤ `MaxAutoApproveUsdMinor` → `approved` + `StripeWithdrawalExecutionService`; manual rail unchanged (`pending_approval` always).
- **FX import** — `FxRateImportWorker` + `EcbFxRateImporter` nightly ECB daily XML → `fx_rate` table (USD cross-rates); `FxRateReadModel` prefers `ops_manual` over `ecb_daily`.
- **Ops FX override** — `GET/POST /api/v1/platform/accounting/fx-rates` gated by `read:platform:accounting:all` / `manage:platform:accounting:all`.
- **Webhooks** — `account.updated` marks `stripe_global` profiles verified; `transfer.created` / `transfer.failed` complete or fail withdrawals.
- **Config (Stripe sandbox required for live payout testing):** `Billing:Stripe` (`SecretKey`, `WebhookSecret`, `PublishableKey`), `Billing:GlobalPayout` (Account Link return/refresh URLs), `Billing:FxRateImport`, `Billing:WithdrawalAutoApprove`.
- **Tests** — `FxRateImportTests`, `WithdrawalRulesTests` (auto-approve threshold), `BalanceAndWithdrawalHandlerTests` (FX min + stripe auto-approve), `PublishFxRateOverrideHandlerTests`.

## Phase 10 notes

- **Chargeback webhook** — `charge.dispute.created` handler bans buyer account (Identity `Account.Ban()`), inserts `BannedPaymentInstrument` from `PaymentTransaction.payment_method_fingerprint`, revokes entitlement + posts `JournalType.Chargeback` reversal (seller bears dispute fee), audits `chargeback_received`. `charge.dispute.closed` logged only.
- **Checkout pre-flight** — `CreateCheckoutSessionHandler` rejects banned accounts; `PaidPurchaseCompletionService` rejects banned fingerprints at payment completion.
- **Org lifecycle** — verified existing: checkout blocks suspended/closed seller orgs; `PendingToAvailableWorker` skips hold release for suspended/closed orgs; `CreateWithdrawalHandler` blocks when `SellerReceivable > 0`; catalog pricing changes blocked on suspend/close (Phase 1).
- **Tests** — `ChargebackHandlerTests`, `PaidPurchaseCompletionServiceTests` (banned fingerprint), `CreateCheckoutSessionHandlerTests` (banned account); receivable withdrawal block covered in `BalanceAndWithdrawalHandlerTests`.

## Phase 5 notes

- **Trust signal source:** Public catalog/discovery DTOs expose seller `trustTier` from `ITenancyOrganizationReadModel` (listing org on releases; managing org on artists). Values: `unverified`, `identityVerified`, `platformVerified` (camelCase JSON).
- **Search ranking:** `CatalogDiscoverySearchRanking` orders results verified-first, then title; `limit` fills verified slots before unverified (`CatalogDiscoveryReadModel.SearchAsync`). Discovery search response still returns `verified` / `unverified` arrays.
- **Browse home:** Recent releases sorted platform-verified first, then by `releaseDate` desc.
- **Consumer UI:** `UnverifiedSellerBadge` on artist and release pages when `trustTier === "unverified"`.
- **Tests:** `CatalogDiscoverySearchRankingTests` (verified-priority within limit).

## Phase 11 notes

- **State pattern** — `PayoutProfile` refactored to `Billing/PayoutProfiles/PayoutProfileState.cs` (follows `WithdrawalRequest` exemplar). `Purchase` / `PaymentTransaction` deferred.
- **Integration tests** — `BillingCommerceFlowTests` (free acquire, checkout auth, refund claim gate, withdrawal without profile).
- **Docs** — `ai-docs/backend/commerce.md`, `ai-docs/frontend/commerce.md`.
- **Test fix** — `Stream_info_unknown_track_returns_problem` asserts `code` not `title`.
- **WithdrawalRequest tests** — `WithdrawalRequestTests` covers valid transitions, rejected transitions, and EF rehydration.
