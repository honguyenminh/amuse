# Amuse DA1 Master Implementation Plan

## 1. Purpose
This plan defines how to implement DA1 for Amuse: a multi-tenant B2B2C music streaming platform with a consumer portal and a business portal.

## 2. Scope Baseline (DA1)
In scope:
- Consumer portal: playback, search, playlist, recommendation, onboarding preference for unverified content.
- Business portal: organization management, member roles, upload flow, release/track management, payout ledger and monthly statements, basic analytics.
- Streaming: DASH primary plus optional HLS output from the same transcode pipeline.
- Auth: JWT access + refresh tokens, org-scoped claims/permissions, Redis blacklist.
- Multi-tenancy: single DB with row-level isolation by organization_id.
- Payments: mock gateway only, double-entry ledger, monthly settlement.
- Recommendation: rule-based + ML.NET nightly retraining.
- Observability: OTLP Collector + Tempo + Loki + Grafana.
- CDN/Storage: Cloudflare-first using R2 as media storage and Cloudflare CDN for delivery.
- Deployment: develop on local K3s, final demo on AKS using CI/CD.

Out of scope (DA2+):
- Full microservice split for all domains.
- Real payment gateway legal onboarding.
- Advanced social/listen-together and campaign tooling.

## 3. Architecture Summary
- Frontends:
  - consumer.amuse: B2C experience.
  - business.amuse: B2B operations.
- Backend API: REST /api/v1.
- Data: PostgreSQL (system of record), Redis (cache + token blacklist + ephemeral ranking cache), Cloudflare R2 (media artifacts).
- Async: transcode queue workers.
- Delivery: Cloudflare Worker validates playback JWT on manifest and segment requests, then proxies to R2.

## 4. Workstreams
1. Domain and data foundation.
2. Identity and authorization.
3. Upload/transcode/distribution.
4. Consumer experience.
5. Business operations and payout.
6. Recommendation and analytics.
7. Observability, performance, and hardening.
8. CI/CD and AKS release.

## 5. Phase Plan

### Phase 0 - Foundation (Week 1)
Goals:
- Repository conventions and baseline architecture.
- Database schema bootstrap.
- Auth skeleton and tenant context middleware.
Deliverables:
- Initial schema migrations.
- API service skeleton.
- Token issuance/refresh flow.
Exit criteria:
- User can authenticate and switch organization context.

### Phase 1 - Core Catalog and B2B Upload (Week 2)
Goals:
- Organization/member management.
- ReleaseGroup, Release, Track CRUD.
- Upload pipeline entry and job dispatch.
Deliverables:
- Business portal pages for upload/catalog/member roles.
- Track lifecycle states: Draft -> Processing -> Ready -> Published/Hidden.
Exit criteria:
- Org admin uploads track and sees processing status updates.

### Phase 2 - Streaming Pipeline (Week 3)
Goals:
- DASH packaging and optional HLS generation.
- Signed playback manifest/token endpoint.
Deliverables:
- Worker-based transcode + packaging.
- Playback URL issuance with quality gating (Free 128kbps, Premium any).
- Cloudflare playback path on cdn.amuse.<domain> with fail-closed edge auth.
Exit criteria:
- Consumer can play published track with adaptive stream.

### Phase 3 - Consumer Features + Recommendation (Week 4)
Goals:
- Search, playlists, recommendation sections.
- Unverified visibility preference captured at onboarding.
Deliverables:
- Verified results section and unverified section behavior.
- Rule-based recommendation service.
- ML.NET nightly training job and model scoring endpoint.
Exit criteria:
- End-to-end discovery and playback flows work for both visibility settings.

### Phase 4 - Payments, Ledger, and Settlement (Week 5)
Goals:
- Mock payment flow.
- Double-entry journal and monthly pro-rata payout.
Deliverables:
- Subscription tiers and entitlement checks.
- Valid stream rule (>=30s) in payout aggregation.
- Monthly statement API and business portal view.
Exit criteria:
- Monthly settlement simulation completes with balanced ledger entries.

### Phase 5 - Reliability, Security, and Observability (Week 6)
Goals:
- Full telemetry and logging.
- Performance and abuse controls.
Deliverables:
- OTLP Collector + Tempo + Loki + Grafana dashboards.
- Moderation workflow: auto-hide at >=5 valid reports plus manual review.
- Load/performance baselines.
Exit criteria:
- NFR targets validated and dashboards available.

### Phase 6 - Release and Demo (Week 7)
Goals:
- CI/CD pipeline to AKS.
- Final hardening and runbook.
Deliverables:
- Build/test/deploy pipeline.
- AKS deployment manifests/helm and smoke tests.
Exit criteria:
- Definition of Done met and demo checklist passed.

## 6. Definition of Done (DA1)
- Tier-1 use cases run end-to-end in demo environment.
- Performance evidence available for:
  - Playback start < 2.5s on broadband.
  - P95 non-stream API latency < 300ms.
  - 5-min track transcode completion < 5 min.
  - Load baseline 500 concurrent listeners + 100 concurrent API users.
- AKS deployment with CI/CD completed.

## 7. Key Business Rules
- Discover promotion gate:
  - (monthly unique listeners >= 200 AND followers >= 5000)
  - OR (monthly unique buyers >= 100 AND followers >= 1000)
  - then manual admin approval required.
- Moderation:
  - Auto-hide track at >=5 valid reports, then manual review.
- Search:
  - Verified section first.
  - Unverified displayed in separate section below.
  - If user enables Allow unverified artists, do not apply ranking penalty.

## 8. Risks and Controls
- Infra overfocus: prioritize feature delivery first; infrastructure complexity after end-to-end flows work.
- Settlement correctness: enforce double-entry invariants and reconciliation tests.
- Recommendation cold start: combine rule-based layer with ML outputs.
- AKS budget/time risk: keep AKS as final-stage deployment, develop on local K3s.
- CDN token/path misconfiguration risk: enforce edge validation tests and purge automation on visibility changes.

## 9. Immediate Next Actions
1. Finalize API and DB contracts from docs.
2. Scaffold backend services and migration set.
3. Implement auth + tenant context before feature endpoints.
4. Build upload/transcode pipeline before advanced recommendation work.
