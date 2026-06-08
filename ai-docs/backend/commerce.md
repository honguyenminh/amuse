# Commerce & billing (backend)

Source of truth: `ads/finance/buying.md`, `ads/finance/payment.md`, `ads/finance/implementation-plan.md`.

## Bounded context

- **Domain:** `backend/src/Amuse.Domain/Billing/`
- **Module (VSA):** `backend/src/Amuse.Modules/Billing/`
- **Scheduler workers:** `PendingToAvailableWorker`, `FxRateImportWorker` (registered via `AddBillingSchedulerWorkers()`)

## Lifecycles — State pattern

Complex aggregates use internal state classes; postgres enums remain the persistence source of truth. After EF load, `_state` is lazily resolved from the stored enum.

| Aggregate | State types | Notes |
| --------- | ----------- | ----- |
| `WithdrawalRequest` | `Billing/Withdrawals/WithdrawalRequestState.cs` | Exemplar |
| `PayoutProfile` | `Billing/PayoutProfiles/PayoutProfileState.cs` | Gate B verification |

Invalid transitions return `Result.Failure(BillingErrors.*)` — never exceptions.

## APIs (org / listener)

| Endpoint | Claim / persona | Purpose |
| -------- | --------------- | ------- |
| `POST /api/v1/billing/acquisitions/free` | Listener | Free PWYW (floor = 0) acquire |
| `POST /api/v1/billing/checkout/sessions` | Listener | Paid Stripe checkout |
| `GET /api/v1/billing/purchases/me` | Listener | My purchases |
| `GET /api/v1/billing/entitlements/ownership` | Listener | Ownership check |
| `GET /api/v1/billing/downloads/tracks/{id}` | Listener | Owner download |
| `GET/PUT /api/v1/billing/payout-profile` | `read:payout:all` / `manage:payout:profile:all` | Gate B profile |
| `POST /api/v1/billing/payout-profile/submit` | `manage:payout:profile:all` | Submit for ops review |
| `POST /api/v1/billing/payout-profile/stripe-account-link` | `manage:payout:profile:all` | Stripe Account Link (`stripe_global` rail) |
| `GET /api/v1/billing/balance` | `read:payout:all` | Seller balance |
| `GET /api/v1/billing/statements` | `read:payout:all` | Allocation statement lines |
| `POST/GET /api/v1/billing/withdrawals` | `manage:payout:withdraw:all` / `read:payout:all` | Withdrawals |
| `POST /api/v1/billing/purchases/{id}/refund` | Platform or seller refund claims | Refunds |
| `POST /api/v1/billing/webhooks/stripe` | Anonymous (signature) | Stripe webhooks |

## Stripe integration

- **Checkout:** `ICheckoutProvider` → `StripeCheckoutProvider`
- **Global payouts:** `IGlobalPayoutProvider` → `StripeGlobalPayoutProvider` (Connect custom account + Account Link + Transfer)
- **Webhooks handled:** `checkout.session.completed`, `charge.refunded`, `charge.dispute.created`, `charge.dispute.closed`, `account.updated`, `transfer.created`, `transfer.failed`

### Withdrawal auto-approve (stripe_global only)

When `PayoutProfile` is verified, rail is `stripe_global`, USD equivalent ≤ `Billing:WithdrawalAutoApprove:MaxAutoApproveUsdMinor`, and `external_recipient_id` is set:

1. `CreateWithdrawalHandler` creates withdrawal in `approved` status
2. `StripeWithdrawalExecutionService` submits outbound transfer and completes ledger on success
3. Above threshold → `pending_approval` (platform ops queue)

`manual_bank` rail always starts in `pending_approval` regardless of amount.

## FX rates

- **Table:** `billing.fx_rate` (`FxRate` aggregate)
- **Nightly import:** `FxRateImportWorker` → `EcbFxRateImporter` (ECB daily XML, USD cross-rates)
- **Usage:** withdrawal minimum USD check, balance USD equivalent display
- **Config:** `Billing:FxRateImport` (`EcbDailyUrl`, `RunIntervalHours`, `SupportedQuoteCurrencies`)

## Chargebacks & bans

On `charge.dispute.created`:

1. Ban buyer account (`Account.Ban()`)
2. Insert `BannedPaymentInstrument` from card fingerprint
3. Revoke entitlement + post chargeback journal
4. Checkout pre-flight rejects banned accounts and fingerprints

## Org lifecycle ledger rules

- Checkout blocks suspended/closed seller orgs
- `PendingToAvailableWorker` skips hold release for suspended/closed orgs
- Withdrawals blocked when `SellerReceivable > 0`
- Catalog pricing changes blocked on suspend/close (Catalog BC)

## Config keys

```json
"Billing": {
  "WithdrawalAutoApprove": { "MaxAutoApproveUsdMinor": 500000, "CooldownDays": 7 },
  "Stripe": { "SecretKey", "WebhookSecret", "PublishableKey" },
  "Checkout": { "SuccessUrl", "CancelUrl" },
  "GlobalPayout": { "AccountLinkReturnUrl", "AccountLinkRefreshUrl" },
  "FxRateImport": { "EcbDailyUrl", "RunIntervalHours", "SupportedQuoteCurrencies" }
}
```

## Tests

- **Domain:** `backend/tests/Amuse.Domain.Tests/Billing/`
- **Handlers:** `backend/tests/Amuse.Modules.Billing.Tests/`
- **Integration:** `backend/tests/Amuse.Api.IntegrationTests/BillingCommerceFlowTests.cs`

## Verification

```bash
cd backend
dotnet build
dotnet test tests/Amuse.Domain.Tests --filter "FullyQualifiedName~Billing"
dotnet test tests/Amuse.Modules.Billing.Tests
dotnet test tests/Amuse.Api.IntegrationTests --filter "FullyQualifiedName~BillingCommerce"
```
