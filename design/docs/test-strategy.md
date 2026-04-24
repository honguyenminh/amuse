# Test Strategy and Acceptance Plan

## 1. Goals
Validate that DA1 is functionally complete, tenant-safe, operationally stable, and meets defined performance targets.

## 2. Test Layers
1. Unit tests:
- business rule calculations (promotion eligibility, valid stream filter, payout math).
- ledger balancing and posting invariants.
- authorization policy checks.

2. Integration tests:
- auth/token refresh/blacklist flows.
- org membership and permission enforcement.
- upload -> queue -> processing state transitions.

3. End-to-end tests:
- consumer playback and playlist flows.
- business upload and payout statement flows.
- moderation auto-hide and manual restore.

4. Performance and load tests:
- API latency baseline.
- playback start and sustained stream behavior.
- transcode SLA under queue pressure.

5. Resilience tests:
- recommendation model unavailable fallback.
- worker retry/idempotency.
- settlement rerun consistency.
- CDN edge validation failures and purge propagation correctness.

## 3. Acceptance Scenarios

### AS-01 Multi-tenant auth and org switching
Given: account belongs to two organizations
When: user switches org context
Then: data and permissions reflect active org only
And: cross-tenant access returns forbidden

### AS-02 Upload to playable stream
Given: creator uploads track master
When: processing completes
Then: DASH manifest exists and is playable
And: optional HLS assets exist when enabled
And: playback requests succeed only through Cloudflare edge path.

### AS-08 Edge token enforcement
Given: playback_jwt is missing, expired, or tampered
When: client requests manifest or segment
Then: Cloudflare edge denies access
And: no direct-origin fallback path is available.

### AS-09 Purge propagation
Given: moderation hide or artifact replacement event
When: purge is triggered
Then: stale media is no longer served from edge cache within expected purge window.

### AS-03 Moderation threshold
Given: a track accumulates 5 valid reports
When: threshold reached
Then: track is auto-hidden and appears in moderation queue

### AS-04 Search section behavior
Given: mixed verified/unverified matches
When: search executed
Then: verified section appears first
And: unverified appears in separate lower section
And: preference ON removes unverified suppression

### AS-05 Recommendation continuity
Given: ML model unavailable
When: recommendation endpoint called
Then: service returns rule-based results with fallback flag

### AS-06 Payment to settlement chain
Given: successful mock subscription payment
When: monthly settlement runs
Then: only valid streams (>=30s) are counted
And: payout statements are generated
And: all journals are balanced

### AS-07 Deployment readiness
Given: tagged release
When: CI/CD pipeline executes
Then: deployment to AKS succeeds
And: smoke tests pass

## 4. Non-Functional Validation
- NFR-001: playback start < 2.5s (synthetic client benchmark).
- NFR-002: p95 non-stream API latency < 300ms (load profile).
- NFR-003: 5-min transcode < 5min (queue benchmark).
- NFR-004: load baseline 500 listeners + 100 API users (soak + burst).
- NFR-010: unauthorized media requests are denied at edge for both manifest and segment paths.

## 5. Performance Profiles
Profile A (API):
- mix: auth 5%, search 35%, playlist 25%, recommendations 20%, misc 15%
- concurrency: 100 API users
- duration: 30 min

Profile B (Playback):
- 500 listener sessions
- staggered starts over 5 min
- sustained duration: 30 min
- collect startup delay and rebuffer metrics

Profile C (Transcode):
- enqueue 50 tracks of 5 min each
- measure queue waiting + process duration

## 6. Quality Gates
Release candidate fails if any condition is true:
- tenant leakage found
- ledger journal imbalance found
- NFR thresholds not met
- CI/CD AKS deployment smoke tests fail

## 7. Test Data Strategy
Dataset target:
- 10,000 tracks
- 1,000 releases
- 500 artists
Generation notes:
- include mix of verified and unverified catalog.
- include varied bitrate assets and durations.

## 8. Reporting Artifacts
- Coverage report (unit/integration).
- Load test report with percentile metrics.
- Settlement reconciliation report.
- AKS deployment and smoke log bundle.
