# Risk Register and Mitigation Plan

## 1. Risk Matrix

| ID | Risk | Probability | Impact | Mitigation | Owner |
|---|---|---|---|---|---|
| R-01 | Scope creep from DA2 features | High | High | Enforce DA1 scope gates and backlog labels | PM/Tech Lead |
| R-02 | Infra complexity delays feature work | Medium | High | Deliver end-to-end product flows before infra hardening | Tech Lead |
| R-03 | Transcode throughput misses SLA | Medium | High | Worker autoscaling, job retry tuning, queue monitoring | Backend |
| R-04 | Cross-tenant data exposure | Low | Critical | Central authz middleware + tenant test suite | Backend/Sec |
| R-05 | Ledger inconsistencies | Medium | Critical | Double-entry invariants and reconciliation tests | Backend/Finance |
| R-06 | Recommendation cold start quality | Medium | Medium | Hybrid ranker (rules + ML), fallback policies | Data/Backend |
| R-07 | Moderation false positives/negatives | Medium | Medium | Review queue tooling and audit trail | Platform Admin |
| R-08 | AKS deployment cost/time overrun | Medium | High | Local K3s dev, AKS only final window | DevOps |
| R-09 | Observability stack instability | Low | Medium | Start with minimal dashboard set and phased expansion | DevOps |
| R-10 | Load test environment mismatch | Medium | Medium | Reproduce prod-like limits before final benchmark | QA/DevOps |
| R-11 | Cloudflare Worker token validation misconfiguration | Medium | High | Edge auth integration tests and staged rollout policy | Backend/DevOps |
| R-12 | Purge propagation delay serving stale media | Medium | High | Tag-based purge automation and validation probes | DevOps |

## 2. Top Operational Risks
1. Settlement run failure near demo window.
2. Queue backlog causing delayed publish.
3. Playback failures due to token/signature misconfiguration.
4. CDN cache stale content after moderation or republish.

## 3. Contingency Actions
- Settlement contingency:
  - rerunnable idempotent settlement by period key.
  - lock payout statement publication until reconciliation passes.
- Transcode contingency:
  - prioritize DASH-only output under load; keep optional HLS feature-gated.
- Playback contingency:
  - fallback to lower bitrate manifest and retry token issuance.
- CDN contingency:
  - trigger forced tag-based purge and temporarily unpublish impacted track until purge verification passes.

## 4. Monitoring Alerts
- Queue backlog > threshold for 10 min.
- Transcode average duration trending above SLA.
- Playback start p95 above 2.5s.
- API latency p95 above 300ms.
- Error spikes on payment callback and settlement jobs.

## 5. Go/No-Go Checklist
Go if:
- all Must flows pass acceptance tests.
- NFR targets pass in benchmark window.
- AKS deploy and smoke test green.
- ledger reconciliation clean for last settlement simulation.

No-Go if:
- any tenant isolation defect remains open.
- any financial integrity defect remains open.
- playback startup metric fails threshold persistently.
