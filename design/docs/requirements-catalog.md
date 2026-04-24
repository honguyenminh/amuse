# Requirements Catalog and Traceability

## 1. Functional Requirement Catalog

| ID | Requirement | Priority | DA Phase | Dependencies | Verification |
|---|---|---|---|---|---|
| FR-001 | JWT login with access+refresh | Must | DA1 | Auth service, account schema | Integration test |
| FR-002 | Multi-organization account membership | Must | DA1 | org/member schema | Integration test |
| FR-003 | Org-scoped claims/permissions | Must | DA1 | token service, auth middleware | Security test |
| FR-004 | Redis token blacklist | Must | DA1 | Redis | Integration test |
| FR-005 | Tenant activation approval | Must | DA1 | admin workflow | UAT |
| FR-006 | Org classes: Indie Group and Backing Organization | Must | DA1 | org schema | Unit + UAT |
| FR-007 | Member invite and role/permission assignment | Must | DA1 | membership endpoints | Integration test |
| FR-008 | ReleaseGroup support | Must | DA1 | catalog schema | Integration test |
| FR-009 | Release/Track CRUD | Must | DA1 | catalog service | Integration test |
| FR-010 | Upload -> processing job flow | Must | DA1 | object storage + queue | End-to-end test |
| FR-011 | DASH + optional HLS packaging | Must | DA1 | worker + ffmpeg + packaging | End-to-end test |
| FR-012 | Auto-hide at >=5 valid reports | Must | DA1 | moderation service | Business rule test |
| FR-013 | Verified-first search sections | Must | DA1 | search index/ranking | API test |
| FR-014 | Unverified preference bypass behavior | Must | DA1 | profile prefs + ranking | API/UAT |
| FR-015 | Discover promotion eligibility + manual approval | Must | DA1 | monthly metrics + admin queue | Batch + UAT |
| FR-016 | Consumer search | Must | DA1 | search service | Perf + integration |
| FR-017 | Playlist create/manage | Must | DA1 | playlist schema/service | Integration test |
| FR-018 | Adaptive playback | Must | DA1 | stream endpoints + manifests | End-to-end test |
| FR-019 | Free/Premium quality entitlements | Must | DA1 | subscription/entitlement | Integration test |
| FR-020 | Rule-based + ML-assisted recommendations | Must | DA1 | rec service + model store | API + quality test |
| FR-021 | Mock payment callback handling | Must | DA1 | payment service | Integration test |
| FR-022 | Double-entry ledger postings | Must | DA1 | ledger schema | Invariant test |
| FR-023 | Monthly pro-rata settlement | Must | DA1 | payout job | Batch reconciliation |
| FR-024 | Valid stream >=30s for payout | Must | DA1 | playback event model | Rule test |
| FR-025 | Monthly payout statements in B2B | Must | DA1 | payout API + UI | UAT |
| FR-026 | Playback analytics collection | Must | DA1 | event ingestion | Data test |
| FR-027 | Nightly ML.NET retraining | Must | DA1 | scheduler + model pipeline | Job test |
| FR-028 | Recommendation fallback when model missing | Must | DA1 | rule-based engine | Chaos test |
| FR-029 | OpenTelemetry logs/metrics/traces | Must | DA1 | OTLP stack | Ops validation |
| FR-030 | CI/CD deployment to AKS | Must | DA1 | pipeline + manifests | Release validation |
| FR-031 | Fail-closed CDN edge access with token on manifest/segment | Must | DA1 | Cloudflare Worker + playback token service | Security test |

## 2. Non-Functional Requirement Catalog

| ID | Requirement | Target | Verification |
|---|---|---|---|
| NFR-001 | Playback start time | < 2.5s | Synthetic playback benchmark |
| NFR-002 | API latency (non-stream) | P95 < 300ms | Load test report |
| NFR-003 | Transcode SLA | 5-min audio < 5 min | Queue/worker benchmark |
| NFR-004 | Load baseline | 500 listeners + 100 API users | Stress/load test |
| NFR-005 | Availability (demo window) | >= 99.0% | SLO monitoring snapshot |
| NFR-006 | Tenant isolation | Zero cross-tenant leakage | Security test suite |
| NFR-007 | Security baseline | TLS + secret hygiene + auth checks | Security checklist |
| NFR-008 | Observability | OTLP + Tempo + Loki + Grafana active | Dashboard evidence |
| NFR-009 | Job reliability | idempotent settlement/training jobs | Re-run consistency test |
| NFR-010 | Edge media authorization | Manifest and segment denied without valid token | Edge security test |

## 3. Requirement to API Mapping (Summary)

| Requirement Cluster | API Domains |
|---|---|
| Identity/Tenancy | /auth, /organizations, /organization-members |
| Catalog/Upload/Moderation | /release-groups, /releases, /tracks, /uploads, /reports, /moderation |
| Playback/Discovery | /playback, /playback/token/refresh, /search, /playlists, /recommendations |
| Billing/Payout | /subscriptions, /payments/mock, /ledger, /settlements, /payouts |
| Analytics/ML/Ops | /analytics, /ml, /health, /cdn/purge |

## 4. Requirement to Schema Mapping (Summary)

| Requirement Cluster | Core Tables |
|---|---|
| Identity/Tenancy | account, organization, organization_member, role_permission, refresh_session, token_blacklist |
| Catalog | release_group, release, track, track_asset, moderation_report, moderation_action |
| Playback | playback_session, playback_event, search_index_view, playlist, playlist_item |
| Finance | subscription, payment_tx, ledger_journal, ledger_entry, settlement_run, payout_statement |
| Recommendation/ML | rec_feature_snapshot, ml_model_version, rec_serving_log |
| Operations | job_run, audit_log |

## 5. Acceptance Test Matrix (Top-level)

| Flow | Must Pass Conditions |
|---|---|
| Multi-org auth | user switches org, claims update, cross-tenant access denied |
| Upload to playable | upload accepted, job completes, manifest issued, consumer playback success |
| Visibility and moderation | verified/unverified sections correct, report threshold auto-hide works |
| Recommendation | endpoint returns ranked items with fallback path |
| Billing and payouts | payment activates plan, ledger balanced, settlement reproducible |
| Deployment | pipeline deploys to AKS and smoke tests pass |
