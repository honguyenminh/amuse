# Work Breakdown Structure (WBS) and Milestones

## 1. Work Packages

### WP-1 Foundation
- WP-1.1 Project skeleton, coding standards, environments
- WP-1.2 Initial DB migrations and tenant model
- WP-1.3 Auth service and token lifecycle
- WP-1.4 Tenant authorization middleware

### WP-2 Business Core (B2B)
- WP-2.1 Organization onboarding and approval workflow
- WP-2.2 Member management and permissions
- WP-2.3 ReleaseGroup/Release/Track CRUD
- WP-2.4 Upload submit and processing status UI

### WP-3 Streaming Pipeline
- WP-3.1 Object storage integration
- WP-3.2 Queue and worker processing
- WP-3.3 DASH packaging
- WP-3.4 Optional HLS output feature flag
- WP-3.5 Playback manifest/token endpoint

### WP-4 Consumer Core (B2C)
- WP-4.1 Onboarding and preferences
- WP-4.2 Search and sectioned ranking behavior
- WP-4.3 Playlist features
- WP-4.4 Playback integration and quality entitlements

### WP-5 Recommendations and Analytics
- WP-5.1 Rule-based ranker
- WP-5.2 Playback event ingestion
- WP-5.3 ML.NET nightly training job
- WP-5.4 Recommendation serving with fallback

### WP-6 Billing and Settlement
- WP-6.1 Mock payment flow and callbacks
- WP-6.2 Subscription and entitlement model
- WP-6.3 Double-entry ledger core
- WP-6.4 Monthly pro-rata settlement and statements

### WP-7 Moderation and Compliance
- WP-7.1 Report submission and validation
- WP-7.2 Auto-hide threshold logic
- WP-7.3 Admin review queue and actions

### WP-8 Operations and Release
- WP-8.1 OpenTelemetry stack deployment
- WP-8.2 Dashboards and alerting
- WP-8.3 Performance/load validation
- WP-8.4 CI/CD to AKS and smoke tests

## 2. Milestones
- M1: Auth and tenant guard complete.
- M2: B2B upload/catalog flow complete.
- M3: Streaming to playable track complete.
- M4: Consumer search/playlists/recommendations complete.
- M5: Billing, ledger, settlement complete.
- M6: Moderation and promotion workflows complete.
- M7: NFR validation complete.
- M8: AKS deployment and final demo readiness complete.

## 3. Dependency Order
1. WP-1 before all other packages.
2. WP-2 can run in parallel with WP-4 after WP-1.
3. WP-3 depends on WP-2 schema and storage setup.
4. WP-5 depends on WP-3 playback event pipeline.
5. WP-6 depends on WP-1 and partial WP-5 analytics.
6. WP-8 starts once services are containerized and tests available.

## 4. Critical Path
WP-1 -> WP-2 -> WP-3 -> WP-4 -> WP-6 -> WP-8

## 5. Exit Criteria per Milestone
- M1: cross-tenant access tests pass.
- M2: upload triggers processing and state tracking.
- M3: end-to-end adaptive playback passes.
- M4: discovery/search behavior matches business rules.
- M5: settlement produces balanced ledger and statements.
- M6: report threshold auto-hide and manual review pass.
- M7: NFR targets achieved under benchmark loads.
- M8: CI/CD deploys AKS successfully and smoke tests pass.
