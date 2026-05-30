# Software Requirements Specification (SRS)
## Project: Amuse DA1
## Version: 1.0
## Date: 2026-04-20

## 1. Introduction
### 1.1 Purpose
This SRS defines functional and non-functional requirements for Amuse DA1, a multi-tenant music streaming system with B2C listener experiences and B2B organization operations.

### 1.2 Scope
Amuse DA1 supports:
- Music ingestion, transcoding, distribution, and playback.
- Multi-organization account access with claims-based authorization.
- Consumer features: search, playlists, recommendations, quality entitlements.
- Business features: upload, catalog management, member role management, payouts.
- Mock subscription billing and monthly settlement.

### 1.3 Definitions
- Tenant: Organization boundary for data and authorization.
- Discover mode: initial state for unverified artists/tracks with controlled visibility.
- Valid stream: playback with >= 30 seconds consumed.
- Settlement cycle: monthly payout aggregation and posting.

### 1.4 References
- Project mail transcript source: raw-convert.md.

## 2. Overall Description
### 2.1 Product Perspective
System components:
- Consumer web app.
- Business web app.
- REST backend (/api/v1).
- Background workers for transcode and batch jobs.
- PostgreSQL, Redis, Cloudflare R2.
- Cloudflare Worker edge authorization on media path.
- Observability stack (OTLP, Tempo, Loki, Grafana).

### 2.2 User Classes
- Listener.
- Organization Admin.
- Organization Member (Creator, Accountant, Analyst).
- Platform Admin (moderation and tenant approvals).

### 2.3 Operating Environment
- Linux-based containers.
- Kubernetes deployment (dev: K3s, demo: AKS).
- Browser clients for both portals.

### 2.4 Constraints
- DA1 must remain implementation-feasible within timeline.
- Payment must be mock-only in DA1.
- Single DB multi-tenant model is mandatory in DA1.

### 2.5 Assumptions and Dependencies
- Cloudflare R2 is available for media storage.
- Cloudflare Worker runtime is available for edge JWT validation.
- FFmpeg-compatible worker environment is available.
- Nightly job window exists for ML.NET retraining.

## 3. External Interface Requirements
### 3.1 User Interfaces
Consumer portal:
- Onboarding includes Allow unverified artists preference.
- Home/search: verified section plus unverified section.
- Playback and playlist management.

Business portal:
- Organization and role management.
- Upload and catalog screens.
- Payout ledger and monthly statement pages.
- Basic analytics dashboard.

### 3.2 Software Interfaces
- PostgreSQL for transactional data.
- Redis for cache, blacklist, and ephemeral ranking state.
- Cloudflare R2 for media assets and stream artifacts.
- Cloudflare CDN and Worker for token-gated media delivery.
- OTLP telemetry collectors and Grafana stack.

### 3.3 Communications Interfaces
- HTTPS REST for all API traffic.
- Queue-based async processing for transcode jobs.

## 4. Functional Requirements

### 4.1 Identity, Access, and Tenancy
FR-001: System shall authenticate users with email/password and issue JWT access and refresh tokens.
Acceptance: successful login returns access token and refresh token/session cookie.

FR-002: System shall support one account belonging to multiple organizations.
Acceptance: user can switch org context without re-authentication.

FR-003: Access token shall carry organization-scoped claims/permissions.
Acceptance: API rejects operations outside token org scope.

FR-004: System shall blacklist revoked JWTs in Redis.
Acceptance: revoked token is rejected before expiration.

FR-005: Backing organizations shall require platform approval before publish and payout operations; indie groups self-activate with restricted capabilities.
Acceptance: pending backing org cannot publish content or execute payout operations; indie group may upload drafts but not publish to public catalog until promotion rules apply.

### 4.2 Organization and Membership
FR-006: System shall support two organization classes: Indie Group and Backing Organization.
Acceptance: class is recorded and visible in organization profile.

FR-007: Org admin shall invite/remove members and assign role + permissions.
Acceptance: changed permissions affect API authorization immediately after token refresh.

### 4.3 Catalog and Upload
FR-008: Business users shall create ReleaseGroup entities.
Acceptance: release may optionally reference a release group.

FR-009: System shall allow CRUD for Release and Track.
Acceptance: business user can create/edit/archive release and tracks in org scope.

FR-010: System shall accept track uploads and create processing jobs.
Acceptance: uploaded track transitions to Processing state with job ID.

FR-011: System shall package DASH output and optional HLS output.
Acceptance: ready track includes R2-backed manifest URLs for generated outputs.

FR-012: System shall apply moderation auto-hide at >=5 valid reports.
Acceptance: reported track becomes hidden and enters manual review queue.

### 4.4 Discovery and Visibility
FR-013: Search results shall display verified content section before unverified section.
Acceptance: query response grouped by section in deterministic order.

FR-014: If Allow unverified artists preference is enabled, system shall bypass unverified penalty behavior.
Acceptance: same query for user with preference ON returns unverified items without suppression.

FR-015: Discover promotion eligibility shall be computed monthly:
- (unique listeners >= 200 and followers >= 5000) OR
- (unique buyers >= 100 and followers >= 1000)
and then manual admin approval required.
Acceptance: eligible artist appears in approval queue; no auto-verify without approval.

### 4.5 Consumer Features
FR-016: Listener shall search tracks/releases/artists.
Acceptance: search response under target latency for indexed data.

FR-017: Listener shall create and manage playlists.
Acceptance: add/remove/reorder operations persist correctly.

FR-018: Listener shall playback published tracks using adaptive streaming.
Acceptance: player requests manifest + segments through Cloudflare path, worker validates JWT on each request, and startup meets NFR thresholds.

FR-019: Free tier shall be capped at 128kbps; Premium tier shall access any available quality.
Acceptance: entitlement enforcement reflected in issued playback JWT and edge representation access.

FR-031: System shall enforce fail-closed media access at CDN edge with no direct-origin fallback in DA1.
Acceptance: failed or missing playback JWT returns denied response and no origin bypass path is exposed.

FR-020: Recommendation service shall provide home/discovery results using rule-based and ML signals.
Acceptance: endpoint returns ranked tracks and logs source contribution.

### 4.6 Payments, Ledger, and Payout
FR-021: System shall process mock subscription payment callbacks.
Acceptance: successful callback posts payment record and activates subscription.

FR-022: System shall maintain double-entry ledger for all financial postings.
Acceptance: each journal is balanced (sum debits equals sum credits).

FR-023: System shall run monthly settlement using pro-rata valid streams.
Acceptance: statements generated with reproducible totals.

FR-024: Valid stream for settlement shall require >= 30 seconds playback.
Acceptance: events under threshold excluded from payout aggregation.

FR-025: Business portal shall display monthly payout statements.
Acceptance: statement data matches settlement journal and payout summary.

### 4.7 Analytics and ML
FR-026: System shall collect playback analytics for stream/session events.
Acceptance: events are queryable by track, artist, organization, and time range.

FR-027: System shall run ML.NET retraining nightly.
Acceptance: new model metadata/version is persisted per successful run.

FR-028: Recommendation endpoint shall fallback to rule-based results when model unavailable.
Acceptance: service continuity maintained with explicit fallback indicator.

### 4.8 Operations
FR-029: System shall emit structured logs, traces, and metrics via OpenTelemetry.
Acceptance: telemetry visible in Grafana/Tempo/Loki within expected delay.

FR-030: CI/CD pipeline shall build, test, and deploy to AKS.
Acceptance: tagged release triggers successful deployment and smoke tests.

## 5. Non-Functional Requirements
NFR-001 Performance: Playback start time < 2.5s on broadband.
NFR-002 Performance: P95 latency < 300ms for non-stream APIs.
NFR-003 Throughput: 5-minute track transcode completion < 5 minutes under normal queue load.
NFR-004 Load: System supports 500 concurrent listeners + 100 concurrent API users in DA1 benchmark.
NFR-005 Availability: demo environment availability >= 99.0% during evaluation window.
NFR-006 Security: tenant isolation enforced by organization scope checks on all tenant-bound entities.
NFR-007 Security: sensitive data encrypted in transit and secrets managed in cluster secret store.
NFR-008 Observability: logs/traces/metrics available through OTLP Collector + Tempo + Loki + Grafana.
NFR-009 Reliability: critical jobs (settlement and training) must be idempotent/re-runnable.
NFR-010 Delivery Security: media manifests and segments must be inaccessible without valid edge JWT.

## 6. Data Requirements
- Tenant-scoped entities must include organization_id where applicable.
- Playback event data retained for settlement and analytics windows.
- Financial journal entries are immutable once posted.
- Report/moderation records must preserve reviewer decisions and audit trail.

## 7. Security and Privacy Requirements
- Claims-based authorization required for all non-public operations.
- Token revocation check via Redis blacklist.
- Minimum PII collection for user accounts.
- Copyright declaration required at upload time.
- Abuse reporting and moderation workflow required.

## 8. Compliance and Policy Scope
In DA1:
- Copyright declaration + report workflow.
- Basic privacy and security controls.
Not in DA1:
- Full regional legal compliance deep-dive and legal filing integrations.

## 9. Deployment and Operations Requirements
- Dev/test deployments run on local K3s.
- Final demonstration deployment runs on AKS.
- Pipeline must include automated tests and smoke checks.
- Runbooks must include incident triage for playback, queue backlog, and settlement job failures.

## 10. Traceability Baseline
Each FR/NFR maps to:
- at least one API endpoint,
- at least one schema entity or job,
- at least one acceptance/performance test.
(Full matrix documented in requirements-catalog.md)
