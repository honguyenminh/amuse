# Schema and Data Model Specification

## 1. Multi-tenant Strategy
- Database model: single PostgreSQL database.
- Isolation strategy: row-level isolation by organization_id for tenant-bound entities.
- Enforcement:
  - API authorization scope checks.
  - Query guard/middleware injecting organization scope.
  - DB indexes with organization_id prefix for scoped lookups.

## 2. Core Entity Groups

### 2.1 Identity and Access
- account
  - id (uuid, pk)
  - email (unique)
  - password_hash
  - status
  - created_at, updated_at

- organization
  - id (uuid, pk)
  - display_name
  - org_class (enum: indie_group, backing_org)
  - lifecycle_status (enum: draft, active, suspended, closed)
  - onboarding_status (enum: not_required, pending_review, approved, rejected)
  - trust_tier (enum: unverified, identity_verified, platform_verified)
  - created_by_account_id (fk -> account.id)
  - approved_at (timestamptz, nullable)
  - approved_by_operator_id (nullable)
  - rejection_reason (nullable)
  - created_at, updated_at (timestamptz)

- organization_member
  - id (uuid, pk)
  - organization_id (fk -> organization.id)
  - account_id (fk -> account.id)
  - role (ADMIN, CREATOR, ACCOUNTANT, ANALYST)
  - permission_json (jsonb)
  - status
  - joined_at
  - unique(organization_id, account_id)

- refresh_session
  - id (uuid, pk)
  - account_id (fk)
  - session_hash
  - expires_at
  - revoked_at

- token_blacklist
  - jti (text, pk)
  - expires_at
  - reason

### 2.2 Catalog
- release_group
  - id (uuid, pk)
  - organization_id (fk)
  - title
  - description
  - created_at

- release
  - id (uuid, pk)
  - organization_id (fk)
  - release_group_id (fk nullable -> release_group.id)
  - title
  - release_type (ALBUM, EP, SINGLE)
  - release_date
  - status (DRAFT, PROCESSING, READY, PUBLISHED, HIDDEN, ARCHIVED)
  - created_at, updated_at

- track
  - id (uuid, pk)
  - organization_id (fk)
  - release_id (fk)
  - title
  - duration_sec
  - explicit_flag
  - status (DRAFT, PROCESSING, READY, PUBLISHED, HIDDEN, ARCHIVED)
  - created_at, updated_at

- track_asset
  - id (uuid, pk)
  - organization_id (fk)
  - track_id (fk)
  - asset_kind (MASTER, DASH_MPD, DASH_SEGMENT, HLS_M3U8, HLS_SEGMENT)
  - storage_provider (R2)
  - object_key
  - codec
  - bitrate_kbps
  - storage_uri
  - cache_tag
  - checksum
  - created_at

- edge_token_audit
  - id (uuid, pk)
  - account_id (fk)
  - track_id (fk)
  - organization_id (fk)
  - token_jti
  - expires_at
  - created_at

- cdn_purge_request
  - id (uuid, pk)
  - trigger_type (MODERATION, REPUBLISH, REPACKAGE, ENTITLEMENT_CHANGE, MANUAL)
  - organization_id (fk nullable)
  - track_id (fk nullable)
  - cache_tag
  - status (PENDING, SUCCESS, FAILED)
  - requested_by_account_id (fk nullable)
  - requested_at
  - completed_at

### 2.3 Discovery, Moderation, and Preferences
- listener_preference
  - account_id (pk/fk)
  - allow_unverified_artists (bool)
  - set_during_onboarding (bool)
  - updated_at

- moderation_report
  - id (uuid, pk)
  - organization_id (fk nullable for platform-wide)
  - track_id (fk)
  - reporter_account_id (fk)
  - reason_code
  - status (OPEN, VALID, INVALID, RESOLVED)
  - created_at

- moderation_action
  - id (uuid, pk)
  - track_id (fk)
  - action_type (AUTO_HIDE, MANUAL_HIDE, RESTORE, REMOVE)
  - actor_account_id (fk nullable for system)
  - note
  - created_at

### 2.4 Playback and Analytics
- playback_session
  - id (uuid, pk)
  - account_id (fk)
  - track_id (fk)
  - started_at
  - ended_at

- playback_event
  - id (bigserial, pk)
  - session_id (fk)
  - account_id (fk)
  - track_id (fk)
  - organization_id (fk)
  - event_type (START, HEARTBEAT, STOP)
  - played_ms
  - timestamp
  - index (track_id, timestamp)

- search_rank_snapshot
  - id (uuid, pk)
  - track_id (fk)
  - section (VERIFIED, UNVERIFIED)
  - score
  - generated_at

- playlist
  - id (uuid, pk)
  - account_id (fk)
  - title
  - visibility
  - created_at, updated_at

- playlist_item
  - id (uuid, pk)
  - playlist_id (fk)
  - track_id (fk)
  - position
  - added_at
  - unique(playlist_id, position)

### 2.5 Payments and Ledger
- subscription
  - id (uuid, pk)
  - account_id (fk)
  - tier (FREE, PREMIUM)
  - status (ACTIVE, PAUSED, CANCELED)
  - starts_at, ends_at

- payment_tx
  - id (uuid, pk)
  - account_id (fk)
  - provider (MOCK)
  - external_ref
  - amount_minor
  - currency
  - status
  - created_at

- ledger_journal
  - id (uuid, pk)
  - journal_type (PAYMENT, SETTLEMENT, ADJUSTMENT)
  - reference_id
  - posted_at

- ledger_entry
  - id (uuid, pk)
  - journal_id (fk)
  - account_code
  - organization_id (fk nullable)
  - direction (DEBIT, CREDIT)
  - amount_minor
  - currency
  - created_at

- settlement_run
  - id (uuid, pk)
  - period_start
  - period_end
  - status
  - executed_at

- payout_statement
  - id (uuid, pk)
  - settlement_run_id (fk)
  - organization_id (fk)
  - valid_streams
  - gross_minor
  - net_minor
  - created_at

### 2.6 Recommendation/ML
- rec_feature_snapshot
  - id (uuid, pk)
  - snapshot_date
  - account_id (fk)
  - track_id (fk)
  - feature_json (jsonb)

- ml_model_version
  - id (uuid, pk)
  - model_name
  - version_tag
  - trained_at
  - metrics_json (jsonb)
  - artifact_uri

- rec_serving_log
  - id (uuid, pk)
  - account_id (fk)
  - request_context_json
  - result_track_ids (jsonb)
  - model_version_id (fk nullable)
  - fallback_used (bool)
  - served_at

## 3. Key Constraints and Rules
1. Tenant guard:
   - all tenant-bound writes require organization_id from token context.
2. Ledger balance:
   - for each journal_id, sum(debit) == sum(credit).
3. Valid stream:
   - payout aggregation uses playback evidence with played_ms >= 30000.
4. Moderation threshold:
   - at least 5 valid reports trigger AUTO_HIDE action.
5. Promotion eligibility (monthly):
   - (unique_listeners >= 200 and followers >= 5000)
   - OR (unique_buyers >= 100 and followers >= 1000)
   - plus manual approval action.
6. Edge delivery:
  - manifests and segments require valid playback JWT at Cloudflare Worker.
  - DA1 policy is fail-closed with no direct origin fallback.

## 4. Indexing Strategy (Minimum)
- organization_member: (organization_id, account_id), (account_id)
- release: (organization_id, status, release_date)
- track: (organization_id, release_id, status)
- playback_event: (track_id, timestamp), (organization_id, timestamp), (account_id, timestamp)
- moderation_report: (track_id, status)
- payout_statement: (settlement_run_id, organization_id)
- track_asset: (track_id, asset_kind, bitrate_kbps)
- cdn_purge_request: (status, requested_at), (track_id, requested_at)

## 5. Partitioning Guidance
- playback_event should be time-partitioned monthly to support analytics and settlement.
- optional archive partitions for old events after settlement retention windows.

## 6. Data Retention (DA1 Baseline)
- playback_event raw: retain minimum 12 months.
- settlement artifacts and ledger: retain indefinitely for financial auditability.
- moderation reports/actions: retain minimum 24 months.
- edge_token_audit and cdn_purge_request: retain minimum 6 months.

## 7. ERD Notes
- Account <-> Organization is many-to-many through organization_member.
- ReleaseGroup belongs to an organization; Release may optionally reference one.
- Track belongs to one Release in DA1.
- Track versioning is intentionally not relationally linked in DA1 (treated as separate track rows).
