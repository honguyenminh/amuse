# Amuse Music Streaming Platform - Data Schema Specification

**Version:** 1.0  
**Date:** 2026-04-24  
**Target Implementation Phase:** DA1 (Foundation)  
**Database:** PostgreSQL

---

## Table of Contents
1. [Design Principles](#design-principles)
2. [Core Entities](#core-entities)
3. [Entity Definitions & Relationships](#entity-definitions--relationships)
4. [Data Types & Constraints](#data-types--constraints)
5. [Key Assumptions](#key-assumptions)
6. [Migration & Extensibility Notes (DA2)](#migration--extensibility-notes-da2)

---

## Design Principles

1. **Role-Based Architecture**: All user types (Listener, Artist, Organization, Admin) are modeled within a unified User → Role hierarchy for flexibility and extensibility.
2. **Soft Delete Strategy**: Deleted records (tracks, artists, users) remain in the database with `is_deleted=true` flag to maintain referential integrity and audit trails.
3. **Explicit Versioning**: Audio track versions (MP3, AAC, FLAC) are modeled as separate `TrackVersion` entities, allowing independent transcode status tracking and codec-specific metadata.
4. **Revenue-First Model**: Full purchase, royalty split, and withdrawal entities are included in DA1 to support direct artist monetization.
5. **Unverified Artist Pathway**: Artists begin as `unverified`, can apply for verification after reaching KPI thresholds, and verification is approved by admin review.
6. **Single Currency**: All pricing is stored in VND; multi-currency support is deferred to DA2.
7. **Playlist Immutability**: Explicit `position` field in `Playlist_Track` join table ensures stable playlist ordering independent of insertion time or database sorting.
8. **User Preferences**: Separate `UserPreference` entity stores user-level settings (audio quality, notifications, privacy) without bloating the main `User` table.
9. **Public Sharing**: Shareable links for playlists/tracks are generated via `PlaylistShare` records; sharing is public by default (no auth required to view shared content).
10. **Soft-Delete Audit Trail**: All deletions are tracked via `is_deleted` and `deleted_at` timestamps.

---

## Core Entities

### User Management Layer

#### 1. **User** (Base Entity)
Central user record for all platform roles.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `user_id` | UUID | PK | Unique identifier |
| `email` | VARCHAR(255) | UNIQUE, NOT NULL | Email address; used for auth |
| `password_hash` | VARCHAR(255) | NOT NULL | Bcrypt hash; never store plaintext |
| `display_name` | VARCHAR(255) | NOT NULL | User's public name |
| `avatar_url` | TEXT | NULL | Profile picture URL |
| `bio` | TEXT | NULL | Short bio/description |
| `created_at` | TIMESTAMP | DEFAULT NOW(), NOT NULL | Account creation timestamp |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update timestamp |
| `is_deleted` | BOOLEAN | DEFAULT false | Soft delete flag |
| `deleted_at` | TIMESTAMP | NULL | Deletion timestamp |
| `last_login` | TIMESTAMP | NULL | Last login timestamp |
| `email_verified` | BOOLEAN | DEFAULT false | Email verification status |
| `email_verified_at` | TIMESTAMP | NULL | Email verification timestamp |

**Indexes:**
- `email` (UNIQUE)
- `is_deleted` (for soft-delete queries)

---

#### 2. **Listener** (User Role)
Profile for end-users consuming music.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `listener_id` | UUID | PK | Unique identifier |
| `user_id` | UUID | FK → User, NOT NULL | Reference to base user |
| `country` | VARCHAR(2) | NULL | ISO 3166-1 alpha-2 code |
| `language_preference` | VARCHAR(5) | NULL | BCP 47 language tag (e.g., `en-US`, `vi-VN`) |
| `subscription_tier` | ENUM | DEFAULT 'free' | Values: `free`, `premium`, `student` |
| `subscription_expires_at` | TIMESTAMP | NULL | Subscription expiry; NULL = active indefinitely |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Profile creation |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |

**Subscription Tiers:**
- `free`: Ads, lower audio quality, limited skips
- `premium`: Ad-free, FLAC support, unlimited skips
- `student`: Discounted premium

---

#### 3. **Artist** (User Role)
Profile for music creators (verified and unverified).

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `artist_id` | UUID | PK | Unique identifier |
| `user_id` | UUID | FK → User, NOT NULL | Reference to base user |
| `organization_id` | UUID | FK → Organization, NULL | Label/Organization (if any); NULL for independent artists |
| `is_verified` | BOOLEAN | DEFAULT false | Verification status |
| `verification_status` | ENUM | NOT NULL | Values: `unverified`, `under_review`, `verified`, `rejected` |
| `applied_for_verification_at` | TIMESTAMP | NULL | When verification was requested |
| `verified_at` | TIMESTAMP | NULL | When admin approved verification |
| `rejection_reason` | TEXT | NULL | Reason for verification rejection |
| `play_count_threshold` | BIGINT | DEFAULT 0 | Total plays across all tracks |
| `follower_count_threshold` | BIGINT | DEFAULT 0 | Total followers |
| `can_monetize` | BOOLEAN | DEFAULT false | Can sell/earn royalties (verified only) |
| `bank_account_registered` | BOOLEAN | DEFAULT false | Bank account for withdrawals |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Profile creation |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |
| `is_deleted` | BOOLEAN | DEFAULT false | Soft delete |
| `deleted_at` | TIMESTAMP | NULL | Deletion timestamp |

**Verification Flow:**
1. Artist starts as `unverified`
2. After KPI thresholds reached (play count, followers), can apply → `under_review`
3. Admin reviews and approves → `verified` (or rejects → `rejected`)
4. Only `verified` artists can monetize

**Indexes:**
- `organization_id`
- `verification_status`
- `is_verified`
- `is_deleted`

---

#### 4. **Organization** (User Role / Entity)
Represents labels, record companies, or artist collectives.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `org_id` | UUID | PK | Unique identifier |
| `user_id` | UUID | FK → User, NULL | Admin user for the org (optional) |
| `name` | VARCHAR(255) | NOT NULL | Organization name |
| `description` | TEXT | NULL | Org description |
| `logo_url` | TEXT | NULL | Logo image URL |
| `website` | VARCHAR(255) | NULL | Official website |
| `country` | VARCHAR(2) | NULL | ISO 3166-1 alpha-2 |
| `is_verified` | BOOLEAN | DEFAULT true | Pre-verified on creation |
| `can_monetize` | BOOLEAN | DEFAULT true | Can directly receive revenues |
| `bank_account_registered` | BOOLEAN | DEFAULT false | Bank account for withdrawals |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Creation timestamp |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |
| `is_deleted` | BOOLEAN | DEFAULT false | Soft delete |
| `deleted_at` | TIMESTAMP | NULL | Deletion timestamp |

**Indexes:**
- `is_deleted`
- `is_verified`

---

#### 5. **Admin** (User Role)
Administrative users with system management privileges.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `admin_id` | UUID | PK | Unique identifier |
| `user_id` | UUID | FK → User, NOT NULL | Reference to base user |
| `role` | ENUM | NOT NULL | Values: `super_admin`, `content_moderator`, `support` |
| `permissions` | TEXT[] | NULL | JSON array of permission flags |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Creation timestamp |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |

**Admin Roles:**
- `super_admin`: Full system access
- `content_moderator`: Approve artists, handle reports
- `support`: User support privileges

---

#### 6. **UserPreference**
Stores user-specific settings without bloating the `User` table.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `preference_id` | UUID | PK | Unique identifier |
| `user_id` | UUID | FK → User, UNIQUE, NOT NULL | One preference per user |
| `preferred_audio_quality` | ENUM | DEFAULT 'high' | Values: `low` (96kbps), `medium` (320kbps), `high` (FLAC) |
| `enable_notifications` | BOOLEAN | DEFAULT true | Push/email notifications |
| `notify_on_artist_release` | BOOLEAN | DEFAULT true | Notify when followed artists release new tracks |
| `notify_on_playlist_update` | BOOLEAN | DEFAULT true | Notify on shared playlist updates |
| `explicit_content_allowed` | BOOLEAN | DEFAULT true | Allow explicit tracks |
| `privacy_level` | ENUM | DEFAULT 'friends' | Values: `public`, `friends`, `private` |
| `playlist_visibility_default` | ENUM | DEFAULT 'private' | Default visibility for new playlists |
| `theme_preference` | VARCHAR(50) | DEFAULT 'system' | UI theme: `light`, `dark`, `system` |
| `language` | VARCHAR(5) | DEFAULT 'en-US' | UI language |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Creation timestamp |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |

**Indexes:**
- `user_id` (UNIQUE)

---

### Music Content Layer

#### 7. **Genre**
Music genre/category taxonomy.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `genre_id` | UUID | PK | Unique identifier |
| `name` | VARCHAR(100) | UNIQUE, NOT NULL | Genre name (e.g., "Electronic", "Hip-Hop") |
| `description` | TEXT | NULL | Genre description |
| `parent_genre_id` | UUID | FK → Genre, NULL | For hierarchical genres (e.g., Trap → Hip-Hop) |
| `icon_url` | TEXT | NULL | Genre icon/image |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Creation timestamp |

**Examples:**
- Rock
  - Alternative Rock
  - Heavy Metal
- Electronic
  - House
  - Techno
- Hip-Hop

**Indexes:**
- `name` (UNIQUE)
- `parent_genre_id`

---

#### 8. **Album**
Collection of tracks released together.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `album_id` | UUID | PK | Unique identifier |
| `artist_id` | UUID | FK → Artist, NOT NULL | Primary artist/creator |
| `title` | VARCHAR(255) | NOT NULL | Album title |
| `description` | TEXT | NULL | Album description |
| `cover_art_url` | TEXT | NULL | Album artwork image URL |
| `release_date` | DATE | NOT NULL | Official release date |
| `album_type` | ENUM | NOT NULL | Values: `album`, `ep`, `single`, `compilation` |
| `total_duration_ms` | BIGINT | NULL | Total duration in milliseconds |
| `language` | VARCHAR(5) | NULL | Primary language(s) |
| `explicit` | BOOLEAN | DEFAULT false | Contains explicit content |
| `is_published` | BOOLEAN | DEFAULT false | Published (visible to public) |
| `published_at` | TIMESTAMP | NULL | Publication timestamp |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Creation timestamp |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |
| `is_deleted` | BOOLEAN | DEFAULT false | Soft delete |
| `deleted_at` | TIMESTAMP | NULL | Deletion timestamp |

**Indexes:**
- `artist_id`
- `release_date`
- `is_published`
- `is_deleted`

---

#### 9. **Track**
Individual music track (song).

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `track_id` | UUID | PK | Unique identifier |
| `artist_id` | UUID | FK → Artist, NOT NULL | Primary artist (creator) |
| `album_id` | UUID | FK → Album, NULL | Album (if part of one) |
| `title` | VARCHAR(255) | NOT NULL | Track title |
| `description` | TEXT | NULL | Track description |
| `genre_id` | UUID | FK → Genre, NULL | Primary genre |
| `duration_ms` | BIGINT | NOT NULL | Track duration in milliseconds |
| `isrc` | VARCHAR(12) | NULL | International Standard Recording Code |
| `explicit` | BOOLEAN | DEFAULT false | Explicit content flag |
| `is_published` | BOOLEAN | DEFAULT false | Visible to public |
| `published_at` | TIMESTAMP | NULL | Publication timestamp |
| `original_file_url` | TEXT | NULL | URL to original uploaded file (object storage) |
| `original_file_size_bytes` | BIGINT | NULL | Original file size |
| `original_file_mime_type` | VARCHAR(50) | NULL | MIME type of uploaded file |
| `price_vnd` | DECIMAL(10, 2) | NULL | Purchase price in VND; NULL = not for sale |
| `allow_download` | BOOLEAN | DEFAULT false | Allow track download for premium users |
| `creation_date` | DATE | NOT NULL | When track was created/recorded |
| `can_be_shared` | BOOLEAN | DEFAULT true | Allow user playlist sharing |
| `created_at` | TIMESTAMP | DEFAULT NOW() | DB creation timestamp |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |
| `is_deleted` | BOOLEAN | DEFAULT false | Soft delete |
| `deleted_at` | TIMESTAMP | NULL | Deletion timestamp |

**Indexes:**
- `artist_id`
- `album_id`
- `genre_id`
- `is_published`
- `is_deleted`
- `created_at` (for trending)

---

#### 10. **TrackVersion**
Encoded versions of a track (MP3, AAC, FLAC, etc.).

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `version_id` | UUID | PK | Unique identifier |
| `track_id` | UUID | FK → Track, NOT NULL | Reference to parent track |
| `codec` | ENUM | NOT NULL | Values: `mp3`, `aac`, `flac`, `opus`, `wav` |
| `bitrate_kbps` | INT | NOT NULL | Bitrate in kbps (e.g., 128, 192, 320, 'lossless') |
| `sample_rate_hz` | INT | NULL | Sample rate in Hz (e.g., 44100, 48000, 96000) |
| `file_url` | TEXT | NOT NULL | URL to encoded file (CDN or object storage) |
| `file_size_bytes` | BIGINT | NOT NULL | File size in bytes |
| `mime_type` | VARCHAR(50) | NOT NULL | MIME type (e.g., `audio/mpeg`, `audio/flac`) |
| `transcode_status` | ENUM | DEFAULT 'pending' | Values: `pending`, `processing`, `completed`, `failed` |
| `transcode_error` | TEXT | NULL | Error message if transcoding failed |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Creation timestamp |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |
| `is_deleted` | BOOLEAN | DEFAULT false | Soft delete |

**Codec & Bitrate Examples:**
- MP3: 128, 192, 320 kbps
- AAC: 128, 192, 256 kbps
- FLAC: lossless (variable bitrate, typically 800-1200 kbps)
- Opus: 64, 96, 128 kbps

**Transcode Pipeline:**
1. User uploads original track (any format)
2. Backend enqueues transcode jobs to RabbitMQ for each codec/bitrate
3. Worker service processes async, updates `TrackVersion.transcode_status`
4. Frontend/API polls or listens to WebSocket for completion

**Indexes:**
- `track_id`
- `codec`
- `transcode_status`

---

### Playback & Engagement Layer

#### 11. **Playlist** (User-Created)
User playlists containing multiple tracks.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `playlist_id` | UUID | PK | Unique identifier |
| `listener_id` | UUID | FK → Listener, NOT NULL | Creator/owner |
| `title` | VARCHAR(255) | NOT NULL | Playlist name |
| `description` | TEXT | NULL | Playlist description |
| `cover_art_url` | TEXT | NULL | Custom cover image URL |
| `is_public` | BOOLEAN | DEFAULT false | Public vs. private |
| `allow_collaboration` | BOOLEAN | DEFAULT false | Others can add tracks |
| `total_duration_ms` | BIGINT | NULL | Total playlist duration |
| `track_count` | INT | DEFAULT 0 | Number of tracks (cached for perf) |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Creation timestamp |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Last update |
| `is_deleted` | BOOLEAN | DEFAULT false | Soft delete |
| `deleted_at` | TIMESTAMP | NULL | Deletion timestamp |

**Indexes:**
- `listener_id`
- `is_public`
- `is_deleted`
- `created_at`

---

#### 12. **Playlist_Track** (Join Table)
Links tracks to playlists with explicit ordering.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `playlist_track_id` | UUID | PK | Unique identifier |
| `playlist_id` | UUID | FK → Playlist, NOT NULL | Reference to playlist |
| `track_id` | UUID | FK → Track, NOT NULL | Reference to track |
| `position` | INT | NOT NULL | Order in playlist (1-indexed) |
| `added_by_listener_id` | UUID | FK → Listener, NULL | User who added the track (for collaborative playlists) |
| `added_at` | TIMESTAMP | DEFAULT NOW() | When track was added |
| `is_deleted` | BOOLEAN | DEFAULT false | Soft delete (track removed from playlist) |

**Unique Constraint:**
- `(playlist_id, track_id)` — one track per playlist (no duplicates)

**Indexes:**
- `playlist_id, position` (for ordered retrieval)
- `track_id`
- `is_deleted`

---

#### 13. **PlaylistShare**
Generates shareable public links for playlists.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `share_id` | UUID | PK | Unique identifier |
| `playlist_id` | UUID | FK → Playlist, NOT NULL | Reference to playlist |
| `share_token` | VARCHAR(32) | UNIQUE, NOT NULL | Random URL-safe token for public link |
| `created_by_listener_id` | UUID | FK → Listener, NOT NULL | User who created the share |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Creation timestamp |
| `expires_at` | TIMESTAMP | NULL | Optional expiry; NULL = never expires |
| `is_active` | BOOLEAN | DEFAULT true | Can deactivate without deletion |

**Public URL Example:**
```
https://amuse.local/share/playlist/{share_token}
```

**Indexes:**
- `share_token` (UNIQUE)
- `playlist_id`
- `is_active`

---

#### 14. **PlaylistFollow** (Listener Engagement)
Tracks which listeners follow/heart playlists.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `follow_id` | UUID | PK | Unique identifier |
| `listener_id` | UUID | FK → Listener, NOT NULL | Follower |
| `playlist_id` | UUID | FK → Playlist, NOT NULL | Followed playlist |
| `followed_at` | TIMESTAMP | DEFAULT NOW() | When follow was added |

**Unique Constraint:**
- `(listener_id, playlist_id)` — one follow per (user, playlist) pair

**Indexes:**
- `listener_id`
- `playlist_id`

---

#### 15. **ArtistFollow** (Listener Engagement)
Tracks which listeners follow artists.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `follow_id` | UUID | PK | Unique identifier |
| `listener_id` | UUID | FK → Listener, NOT NULL | Follower |
| `artist_id` | UUID | FK → Artist, NOT NULL | Followed artist |
| `followed_at` | TIMESTAMP | DEFAULT NOW() | When follow was added |

**Unique Constraint:**
- `(listener_id, artist_id)` — one follow per (user, artist) pair

**Indexes:**
- `listener_id`
- `artist_id`

---

#### 16. **TrackFavorite** (Listener Engagement)
Tracks which listeners have liked/favorited tracks.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `favorite_id` | UUID | PK | Unique identifier |
| `listener_id` | UUID | FK → Listener, NOT NULL | User who liked |
| `track_id` | UUID | FK → Track, NOT NULL | Liked track |
| `favorited_at` | TIMESTAMP | DEFAULT NOW() | When favorited |

**Unique Constraint:**
- `(listener_id, track_id)` — one favorite per (user, track) pair

**Indexes:**
- `listener_id`
- `track_id`

---

### Purchase & Revenue Layer

#### 17. **Purchase**
Records individual track/album purchases.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `purchase_id` | UUID | PK | Unique identifier |
| `listener_id` | UUID | FK → Listener, NOT NULL | Buyer |
| `track_id` | UUID | FK → Track, NULL | If track purchase (one of: track or album) |
| `album_id` | UUID | FK → Album, NULL | If album purchase |
| `price_vnd` | DECIMAL(10, 2) | NOT NULL | Final price paid (VND) |
| `payment_method` | ENUM | NOT NULL | Values: `credit_card`, `bank_transfer`, `wallet`, `promotional` |
| `transaction_id` | VARCHAR(255) | UNIQUE, NULL | External payment provider transaction ID |
| `status` | ENUM | DEFAULT 'completed' | Values: `pending`, `completed`, `failed`, `refunded` |
| `purchased_at` | TIMESTAMP | DEFAULT NOW() | Purchase timestamp |
| `refunded_at` | TIMESTAMP | NULL | Refund timestamp (if applicable) |

**Constraint:** Either `track_id` OR `album_id` must be NOT NULL (not both).

**Indexes:**
- `listener_id`
- `track_id`
- `album_id`
- `status`
- `purchased_at`

---

#### 18. **RoyaltySplit**
Defines how revenue from track/album sales is distributed among artist(s) and organization(s).

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `split_id` | UUID | PK | Unique identifier |
| `track_id` | UUID | FK → Track, NULL | If track-level split |
| `album_id` | UUID | FK → Album, NULL | If album-level split |
| `artist_id` | UUID | FK → Artist, NULL | Artist receiving share |
| `organization_id` | UUID | FK → Organization, NULL | Organization receiving share |
| `percentage` | DECIMAL(5, 2) | NOT NULL | Revenue share percentage (0-100) |
| `created_at` | TIMESTAMP | DEFAULT NOW() | When split was created |
| `effective_from` | DATE | NOT NULL | Date split becomes active |
| `effective_to` | DATE | NULL | Date split expires (NULL = ongoing) |

**Constraint:** One of `artist_id` OR `organization_id` must be NOT NULL (not both).

**Constraint:** Sum of `percentage` for a given track/album should = 100.

**Example:**
```
Track "Song A" (artist: Alice)
- Artist Alice: 80%
- Organization (Label): 20%
Total: 100%
```

**Indexes:**
- `track_id`
- `album_id`
- `artist_id`
- `organization_id`

---

#### 19. **Withdrawal**
Request by artist/organization to withdraw earned money.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `withdrawal_id` | UUID | PK | Unique identifier |
| `artist_id` | UUID | FK → Artist, NULL | Artist requesting withdrawal |
| `organization_id` | UUID | FK → Organization, NULL | Organization requesting withdrawal |
| `amount_vnd` | DECIMAL(15, 2) | NOT NULL | Amount requested (VND) |
| `bank_account_last4` | VARCHAR(4) | NULL | Last 4 digits of receiving account |
| `status` | ENUM | DEFAULT 'pending' | Values: `pending`, `approved`, `processing`, `completed`, `failed`, `cancelled` |
| `requested_at` | TIMESTAMP | DEFAULT NOW() | Request timestamp |
| `approved_at` | TIMESTAMP | NULL | Admin approval timestamp |
| `completed_at` | TIMESTAMP | NULL | Completion timestamp |
| `failure_reason` | TEXT | NULL | Reason for failure (if applicable) |

**Constraint:** One of `artist_id` OR `organization_id` must be NOT NULL.

**Indexes:**
- `artist_id`
- `organization_id`
- `status`
- `requested_at`

---

### Moderation & Reporting Layer

#### 20. **Report**
User reports for content violations (copyright, explicit content, spam, etc.).

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `report_id` | UUID | PK | Unique identifier |
| `reporter_id` | UUID | FK → Listener, NOT NULL | User filing the report |
| `track_id` | UUID | FK → Track, NULL | Reported track (one of: track, album, playlist) |
| `album_id` | UUID | FK → Album, NULL | Reported album |
| `playlist_id` | UUID | FK → Playlist, NULL | Reported playlist |
| `reason` | ENUM | NOT NULL | Values: `copyright_violation`, `explicit_content_mislabeled`, `spam`, `malicious`, `other` |
| `description` | TEXT | NULL | Additional context |
| `status` | ENUM | DEFAULT 'pending' | Values: `pending`, `under_review`, `resolved`, `dismissed`, `appealed` |
| `severity` | ENUM | DEFAULT 'low' | Values: `low`, `medium`, `high`, `critical` |
| `admin_notes` | TEXT | NULL | Admin notes during review |
| `action_taken` | ENUM | NULL | Values: `no_action`, `warning`, `suppress`, `remove`, `ban_artist`, `ban_user` |
| `reported_at` | TIMESTAMP | DEFAULT NOW() | Report timestamp |
| `reviewed_at` | TIMESTAMP | NULL | Admin review timestamp |
| `reviewed_by_admin_id` | UUID | FK → Admin, NULL | Admin who reviewed |

**Constraint:** Exactly one of `track_id`, `album_id`, `playlist_id` must be NOT NULL.

**Aggregation:** If a single track receives N reports above a threshold (e.g., 5+ high severity), auto-suppress.

**Indexes:**
- `reporter_id`
- `track_id`
- `album_id`
- `playlist_id`
- `status`
- `reported_at`

---

### Playback Tracking (Optional DA1 / Primary DA2)

#### 21. **PlaybackEvent** (Minimal in DA1)
Records when/where listeners play tracks (supports statistics, recommendations, trending).

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `event_id` | UUID | PK | Unique identifier |
| `listener_id` | UUID | FK → Listener, NOT NULL | User who played |
| `track_id` | UUID | FK → Track, NOT NULL | Track played |
| `session_id` | VARCHAR(64) | NOT NULL | Playback session identifier |
| `played_at` | TIMESTAMP | NOT NULL | When track was played |
| `duration_played_ms` | BIGINT | NOT NULL | How long listener played (ms) |
| `device_type` | ENUM | NULL | Values: `web`, `mobile`, `desktop`, `embedded` |
| `country` | VARCHAR(2) | NULL | ISO 3166-1 alpha-2 (geolocation) |

**Purpose in DA1:**
- Track trending (play count)
- Basic artist analytics
- Feed for recommendations

**Indexes:**
- `listener_id`
- `track_id`
- `played_at` (for time-range aggregations)

---

## Data Types & Constraints

### Standard Types
- **UUID**: Universally unique identifier (128-bit)
- **VARCHAR(n)**: Variable-length string, max n characters
- **TEXT**: Large text field (no length limit)
- **TIMESTAMP**: Date + time with timezone (UTC assumed)
- **DATE**: Date only (YYYY-MM-DD)
- **INT**: 32-bit signed integer
- **BIGINT**: 64-bit signed integer
- **DECIMAL(p, s)**: Fixed-point decimal (p total digits, s after decimal)
- **BOOLEAN**: true/false
- **ENUM**: Predefined set of values (database-level)
- **UUID[]** / **TEXT[]**: Arrays (PostgreSQL-specific)

### Soft Delete Convention
Every entity with `is_deleted`:
- Must also have `deleted_at` TIMESTAMP
- Queries should filter `WHERE is_deleted = false` by default
- Supports audit trails and recovery

### Constraints
- **NOT NULL**: Column must always have a value
- **UNIQUE**: No duplicate values across table
- **PRIMARY KEY (PK)**: Unique identifier for row
- **FOREIGN KEY (FK)**: References another table's PK
- **DEFAULT**: Default value if not specified
- **CHECK**: Row-level constraint (e.g., percentage must be 0-100)

---

## Key Assumptions

1. **Single Organization Per Artist**: An Artist can belong to at most one Organization (Label). Future DA2 can support many-to-many if needed.

2. **No Track Collaborators in DA1**: Tracks have a single primary artist. Featuring artists, producers, composers are deferred to DA2.

3. **Public Sharing by Default**: Shared playlists require no authentication to view. Access control (private sharing) is deferred to DA2.

4. **VND-Only Pricing**: All prices and revenues are stored in Vietnamese Dong (VND). Multi-currency support is deferred to DA2.

5. **Explicit Audio Versioning**: Multiple `TrackVersion` records per Track allow independent transcode workflows. Codec/bitrate combinations are pre-defined (MP3 128/192/320, AAC 128/192/256, FLAC lossless, etc.).

6. **User Preferences as Separate Entity**: To avoid bloating the `User` table, settings (audio quality, notifications, privacy) are stored in `UserPreference` with a 1:1 relationship.

7. **Admin Approval for Verification**: Artists must apply for verification; only admins can approve. KPI thresholds are configuration-based (not hard-coded in schema).

8. **Soft Deletes for Audit**: Deleted records remain in the database with timestamps, enabling audit trails, recovery, and referential integrity.

9. **Purchases Support Both Tracks and Albums**: A `Purchase` record is for either a track OR an album (one-to-many).

10. **Royalty Splits are Time-Bound**: Royalty splits can have effective dates, allowing changes to revenue distribution over time without losing historical records.

11. **Reports Trigger Auto-Moderation at Threshold**: When a track receives N high-severity reports, it can be auto-suppressed pending admin review.

12. **Playback Events are High-Volume**: `PlaybackEvent` is not queried in real-time during playback but batched for analytics. Consider archiving old events to a data warehouse in DA2.

---

## Migration & Extensibility Notes (DA2)

### Planned DA2 Extensions

#### A. **System-Generated Playlists**
```sql
ALTER TABLE Playlist ADD COLUMN playlist_type ENUM DEFAULT 'user';
-- Values: 'user', 'trending', 'recommended', 'discover'
-- System playlists have curator_id instead of listener_id
```

#### B. **Track Collaborators**
```sql
CREATE TABLE TrackContributor (
    contributor_id UUID PRIMARY KEY,
    track_id UUID NOT NULL REFERENCES Track,
    artist_id UUID NOT NULL REFERENCES Artist,
    role ENUM NOT NULL, -- 'primary', 'featuring', 'producer', 'composer', etc.
    order_index INT,
    UNIQUE (track_id, artist_id, role)
);
```

#### C. **Time-Synced Lyrics**
```sql
CREATE TABLE TrackLyrics (
    lyrics_id UUID PRIMARY KEY,
    track_id UUID NOT NULL UNIQUE REFERENCES Track,
    source ENUM, -- 'user_submitted', 'genius', 'musixmatch', etc.
    language VARCHAR(5),
    created_at TIMESTAMP
);

CREATE TABLE LyricsLine (
    line_id UUID PRIMARY KEY,
    lyrics_id UUID NOT NULL REFERENCES TrackLyrics,
    timestamp_ms BIGINT, -- Millisecond offset
    text TEXT,
    UNIQUE (lyrics_id, timestamp_ms)
);
```

#### D. **Multi-Currency Support**
```sql
ALTER TABLE Purchase ADD COLUMN currency_code VARCHAR(3) DEFAULT 'VND';
ALTER TABLE Withdrawal ADD COLUMN currency_code VARCHAR(3) DEFAULT 'VND';
ALTER TABLE RoyaltySplit ADD COLUMN currency_code VARCHAR(3) DEFAULT 'VND';

CREATE TABLE ExchangeRate (
    rate_id UUID PRIMARY KEY,
    from_currency VARCHAR(3),
    to_currency VARCHAR(3),
    rate DECIMAL(10, 6),
    effective_date DATE,
    UNIQUE (from_currency, to_currency, effective_date)
);
```

#### E. **Advanced Statistics & Analytics**
```sql
CREATE TABLE ArtistAnalytics (
    analytics_id UUID PRIMARY KEY,
    artist_id UUID NOT NULL REFERENCES Artist,
    date DATE,
    total_plays BIGINT,
    total_revenue DECIMAL(15, 2),
    unique_listeners INT,
    UNIQUE (artist_id, date)
);
```

#### F. **Microservices Separation**
- **Statistics Service**: Own DB for PlaybackEvent and analytics (time-series DB, e.g., ClickHouse)
- **Payment Service**: Separate DB for Purchase, Withdrawal, RoyaltySplit
- **Moderation Service**: Separate schema for Report, Report appeals, moderation history

---

## Summary

This schema provides a **complete, production-ready data model** for DA1 with:
- ✅ All core roles (Listener, Artist, Organization, Admin)
- ✅ Full purchase and royalty flow
- ✅ Verification workflow for artists
- ✅ Soft-delete audit trails
- ✅ Explicit audio versioning for transcode support
- ✅ User engagement tracking (follows, favorites, playlists)
- ✅ Content moderation and reporting
- ✅ Extensibility hooks for DA2 (system playlists, collaborators, lyrics, multi-currency, analytics)

**Next Steps:**
1. Create PostgreSQL migration files
2. Add indexes and constraints
3. Configure audit logging at DB level
4. Set up test fixtures for unit testing
5. Define API contracts (OpenAPI/Swagger) based on this schema
