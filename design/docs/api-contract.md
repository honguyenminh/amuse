# API Contract Outline (REST /api/v1)

## 1. Conventions
- Base path: /api/v1
- Auth: Bearer JWT access token
- Tenant context: organization_id claim inside token; optional org switch endpoint.
- Error envelope:
  - code
  - message
  - details (optional)
  - trace_id

## 2. Identity and Organization

### POST /auth/login
Request: email, password
Response: access_token, refresh_token/session

### POST /auth/refresh
Request: refresh token/session
Response: new access token

### POST /auth/logout
Action: revoke session and blacklist current token jti.

### GET /organizations
Return organizations user belongs to.

### POST /organizations
Create organization (status PENDING approval).

### GET /organizations/{id}
Get organization profile and approval state.

### POST /organizations/{id}/members/invite
Invite account and assign role/permissions.

### PATCH /organizations/{id}/members/{memberId}
Update role/permissions.

### DELETE /organizations/{id}/members/{memberId}
Remove member.

## 3. Catalog and Upload

### POST /release-groups
Create release group.

### GET /release-groups
List release groups in org scope.

### POST /releases
Create release; optional release_group_id.

### GET /releases
List releases with filters.

### PATCH /releases/{releaseId}
Update release metadata/state.

### POST /tracks
Create track metadata row.

### POST /tracks/{trackId}/upload-url
Issue signed upload URL for master media.

### POST /tracks/{trackId}/submit-upload
Confirm upload and enqueue transcode job.

### GET /tracks/{trackId}
Track detail with processing/status.

### GET /tracks/{trackId}/assets
List generated manifests/segments metadata.

## 4. Playback and Discovery

### GET /search
Query params: q, section, page, page_size
Response sections:
- verified_items
- unverified_items
Behavior:
- if allow_unverified_artists = true, no unverified penalty.

### GET /playback/{trackId}/manifest
Returns Cloudflare CDN manifest endpoint/URL plus playback JWT according to entitlement.

Response payload includes:
- manifest_url (cdn.amuse.<domain>),
- playback_jwt,
- expires_at,
- max_bitrate_kbps.

### POST /playback/token/refresh
Refresh playback token for long sessions without requiring full auth refresh.

### POST /playback/events
Ingest playback event batch.

### CRUD /playlists
- POST /playlists
- GET /playlists
- PATCH /playlists/{id}
- DELETE /playlists/{id}

### POST /playlists/{id}/items
Add track to playlist.

### PATCH /playlists/{id}/items/reorder
Reorder playlist entries.

### GET /recommendations/home
Returns ranked recommendations; includes fallback flag when ML unavailable.

## 5. Moderation and Promotion

### POST /reports
Create moderation report for track.

### GET /moderation/queue
Admin queue of reported/hidden content.

### POST /moderation/actions
Actions: MANUAL_HIDE, RESTORE, REMOVE, VALIDATE_REPORT.

### GET /artists/promotion-eligibility
Monthly eligibility summary for discover promotion.

### POST /artists/{artistId}/apply-verification
Submit manual verification application when thresholds met.

### POST /admin/artists/{artistId}/approve-verification
Admin approval.

## 6. Billing, Ledger, and Payout

### POST /subscriptions/checkout/mock
Start mock checkout flow.

### POST /payments/mock/callback
Mock provider callback.

### GET /subscriptions/me
Current tier and entitlement.

### GET /ledger/journals
Organization or admin-visible journal list.

### GET /ledger/journals/{journalId}
Journal details and balanced entries.

### POST /settlements/run
Admin/manual trigger for monthly settlement.

### GET /payouts/statements
Organization payout statements.

### GET /payouts/statements/{statementId}
Detailed statement lines.

## 7. Analytics and ML

### GET /analytics/overview
Basic metrics: plays, listeners, revenue, trend.

### POST /ml/train/nightly
Trigger nightly training (usually scheduler-owned).

### GET /ml/models/current
Current model metadata and metrics.

## 8. Operational Endpoints

### GET /health/live
Liveness probe.

### GET /health/ready
Readiness probe.

### GET /observability/info
Build/version/trace metadata for diagnostics.

### POST /cdn/purge
Admin/internal endpoint to request Cloudflare cache purge by cache tag or asset scope.

## 9. Idempotency Requirements
- /payments/mock/callback must support idempotency by external_ref.
- /settlements/run must be idempotent per period key.
- upload submit should be idempotent per track + upload token.
- /cdn/purge must be idempotent per trigger key and asset scope.

## 10. Authorization Rules (Examples)
- Organization-bound endpoints require matching organization_id claim.
- Moderation and approval endpoints require platform admin scope.
- Ledger read endpoints require ADMIN or ACCOUNTANT role in organization.
- Playback endpoints must enforce track state visibility and entitlement before issuing playback_jwt.
