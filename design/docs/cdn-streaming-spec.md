# Cloudflare-First Streaming Specification (DA1)

## 1. Scope
This document specifies the Cloudflare-first media delivery path for DA1, including R2 storage, Worker-based JWT validation, DASH/HLS dual output, cache policy, purge triggers, and fail-closed behavior.

## 2. Locked Decisions
- Primary media storage and delivery: Cloudflare R2 + Cloudflare CDN.
- Playback domain: cdn.amuse.<domain>.
- Streaming protocols: DASH primary, HLS optional from the same transcode pipeline.
- Playback authorization at edge: Cloudflare Worker validates JWT and proxies/rewrites to R2.
- Token requirement: token required on every segment request.
- Token TTL baseline: 60 minutes.
- Fallback policy: fail closed, no direct origin fallback in DA1.
- Cache policy: short TTL for manifests, long TTL for segments.
- Purge triggers:
  - moderation hide/remove,
  - republish state change,
  - artifact replacement,
  - entitlement policy change,
  - manual admin purge.

## 3. Architecture
Control plane:
- Backend API issues signed playback JWT and enforces entitlement and visibility checks.

Data plane:
- Cloudflare Worker validates playback JWT.
- Worker forwards validated requests to R2 object paths.
- Client downloads manifests and segments from cdn.amuse.<domain> only.

Storage path conventions:
- r2://media/{organization_id}/{track_id}/dash/manifest.mpd
- r2://media/{organization_id}/{track_id}/dash/{representation}/{segment}.m4s
- r2://media/{organization_id}/{track_id}/hls/master.m3u8
- r2://media/{organization_id}/{track_id}/hls/{variant}/{segment}.ts

## 4. End-to-End Flows

### 4.1 Upload to Publish
1. Creator requests upload URL.
2. Master audio uploaded.
3. Backend enqueues transcode job.
4. Worker generates DASH artifacts and optional HLS artifacts.
5. Artifacts uploaded to R2 and cataloged in track_asset.
6. Publish transition updates visibility and allows playback token issuance.

### 4.2 Playback
1. Client calls playback manifest API.
2. Backend verifies:
   - track state is playable,
   - moderation does not hide content,
   - user entitlement (Free 128kbps cap, Premium unrestricted),
   - org/user access policy.
3. Backend returns manifest URL on cdn.amuse.<domain> plus playback JWT.
4. Client requests manifest and segments using playback JWT.
5. Cloudflare Worker validates JWT on every request.
6. Worker proxies to R2 object.
7. Playback events posted to backend for analytics and payout.

## 5. JWT Contract for Edge Validation
Required claims:
- iss: backend issuer.
- aud: amuse-cdn.
- sub: account_id.
- tid: track_id.
- org: organization_id.
- tier: FREE or PREMIUM.
- max_bitrate_kbps: effective quality cap from entitlement.
- scopes: [manifest:read, segment:read].
- jti: token ID.
- iat, exp.

Validation rules in Worker:
1. Signature valid.
2. Time window valid (iat/exp).
3. aud equals amuse-cdn.
4. Requested path track_id matches tid.
5. Requested representation bitrate <= max_bitrate_kbps.
6. On failure return 401/403 and do not contact R2.

## 6. DASH and HLS Strategy
- DASH is default client path.
- HLS output is generated behind feature flag for compatibility.
- Asset registration requires protocol, bitrate, codec, and object key metadata.
- Player capability selection:
  - default to DASH-compatible clients,
  - fallback to HLS-capable player path when enabled.

## 7. Cache Policy

| Asset Type | Example | Cache TTL | Notes |
|---|---|---|---|
| DASH manifest | .mpd | short (for DA1 baseline: 60s to 300s) | Frequent updates after publish/repackage |
| HLS manifest | .m3u8 | short (for DA1 baseline: 60s to 300s) | Keep in sync with latest variants |
| Segments | .m4s/.ts | long (for DA1 baseline: 1d to 7d) | Immutable artifact keys recommended |

Implementation notes:
- Prefer immutable segment naming to reduce purge pressure.
- Use cache tags keyed by track_id and release_id for selective invalidation.

## 8. Purge Model
Purge API operations:
- Purge by cache tag for track-level changes.
- Purge by prefix for artifact replacement.

Purge triggers:
1. Moderation hide/remove.
2. Track republish or visibility state change.
3. Artifact replacement after retranscode.
4. Entitlement policy change impacting representation access.
5. Manual admin purge.

## 9. Security Model
- Fail-closed delivery: no direct origin URL fallback.
- Private bucket policy: R2 objects not publicly exposed without Worker gate.
- Token required per segment request.
- JWT signing key rotation policy must be supported.
- Playback token minting denied for non-published or hidden tracks.

## 10. API Surface Impact
- GET /playback/{trackId}/manifest returns:
  - protocol-preferred manifest URL on cdn.amuse.<domain>,
  - playback_jwt,
  - expires_at,
  - effective quality/bitrate constraints.
- Optional: POST /playback/token/refresh for long sessions.
- POST /cdn/purge (admin/internal) for manual and event-driven invalidation.

## 11. Data Model Impact
track_asset requires metadata to support CDN operations:
- storage_provider,
- object_key,
- cache_tag,
- protocol,
- bitrate_kbps,
- checksum.

## 12. Observability
Capture and correlate:
- token issuance events,
- edge auth failures,
- manifest/segment hit rates,
- cache hit ratio,
- playback startup and rebuffer metrics,
- purge execution results.

## 13. Test Requirements
Mandatory test scenarios:
1. Valid JWT serves manifest and segments.
2. Expired JWT fails on edge.
3. Path tampering (track mismatch) denied.
4. Bitrate over-entitlement denied.
5. Moderation hide immediately blocks playback after purge.
6. Repackage flow serves fresh manifests/segments after purge.

## 14. DA1 Limits
- No direct-origin fallback.
- No Cloudflare WAF/rate-limiting dependency for correctness (app-layer only in DA1).
- No geo-restriction rules in DA1.
