---
config:
    ER:
        layoutDirection: TB
    title: Amuse Music Streaming - Data Schema ER Diagram
---
erDiagram
    %% USER MANAGEMENT LAYER
    USER ||--o| LISTENER : has
    USER ||--o| ARTIST : has
    USER ||--o| ADMIN : has
    USER ||--|| USERPREFERENCE : defines
    ARTIST }o--|| ORGANIZATION : "belongs_to\n(at_most_one)"
    
    %% MUSIC CONTENT LAYER
    ARTIST ||--o{ ALBUM : creates
    ALBUM ||--o{ TRACK : contains
    GENRE ||--o{ TRACK : categorizes
    TRACK ||--o{ TRACKVERSION : "has_\n(mp3/aac/flac)"
    
    %% ENGAGEMENT LAYER
    LISTENER ||--o{ PLAYLIST : creates
    PLAYLIST ||--o{ PLAYLIST_TRACK : "contains_\n(ordered)"
    TRACK }o--o{ PLAYLIST : "added_to\n(join_table)"
    LISTENER ||--o{ PLAYLISTSHARE : creates
    PLAYLIST ||--o{ PLAYLISTSHARE : "shared_via"
    LISTENER ||--o{ PLAYLISTFOLLOW : "follows"
    PLAYLIST }o--o{ LISTENER : "followed_by\n(join_table)"
    LISTENER ||--o{ ARTISTFOLLOW : "follows"
    ARTIST }o--o{ LISTENER : "followed_by\n(join_table)"
    LISTENER ||--o{ TRACKFAVORITE : "likes"
    TRACK }o--o{ LISTENER : "liked_by\n(join_table)"
    
    %% PURCHASE & REVENUE LAYER
    LISTENER ||--o{ PURCHASE : makes
    PURCHASE }o--|| TRACK : "purchases"
    PURCHASE }o--|| ALBUM : "purchases"
    TRACK ||--o{ ROYALTYSPLIT : "defines_\nrevenue"
    ALBUM ||--o{ ROYALTYSPLIT : "defines_\nrevenue"
    ROYALTYSPLIT }o--|| ARTIST : "pays"
    ROYALTYSPLIT }o--|| ORGANIZATION : "pays"
    ARTIST ||--o{ WITHDRAWAL : requests
    ORGANIZATION ||--o{ WITHDRAWAL : requests
    
    %% MODERATION LAYER
    LISTENER ||--o{ REPORT : files
    TRACK }o--o| REPORT : "reported_for\n(join_table)"
    ALBUM }o--o| REPORT : "reported_for\n(join_table)"
    PLAYLIST }o--o| REPORT : "reported_for\n(join_table)"
    ADMIN ||--o{ REPORT : reviews
    
    %% ANALYTICS & TRACKING
    LISTENER ||--o{ PLAYBACKEVENT : triggers
    TRACK ||--o{ PLAYBACKEVENT : "played_in"
    
    %% ENTITY DEFINITIONS
    USER {
        uuid user_id PK
        string email UK
        string password_hash
        string display_name
        string avatar_url
        text bio
        boolean email_verified
        timestamp created_at
        timestamp updated_at
        boolean is_deleted
        timestamp deleted_at
        timestamp last_login
    }
    
    LISTENER {
        uuid listener_id PK
        uuid user_id FK
        string country
        string language_preference
        enum subscription_tier
        timestamp subscription_expires_at
        timestamp created_at
        timestamp updated_at
    }
    
    ARTIST {
        uuid artist_id PK
        uuid user_id FK
        uuid organization_id FK
        boolean is_verified
        enum verification_status
        timestamp applied_for_verification_at
        timestamp verified_at
        string rejection_reason
        bigint play_count_threshold
        bigint follower_count_threshold
        boolean can_monetize
        boolean bank_account_registered
        timestamp created_at
        timestamp updated_at
        boolean is_deleted
        timestamp deleted_at
    }
    
    ORGANIZATION {
        uuid org_id PK
        uuid user_id FK
        string name
        text description
        string logo_url
        string website
        string country
        boolean is_verified
        boolean can_monetize
        boolean bank_account_registered
        timestamp created_at
        timestamp updated_at
        boolean is_deleted
        timestamp deleted_at
    }
    
    ADMIN {
        uuid admin_id PK
        uuid user_id FK
        enum role
        text[] permissions
        timestamp created_at
        timestamp updated_at
    }
    
    USERPREFERENCE {
        uuid preference_id PK
        uuid user_id FK
        enum preferred_audio_quality
        boolean enable_notifications
        boolean notify_on_artist_release
        boolean notify_on_playlist_update
        boolean explicit_content_allowed
        enum privacy_level
        enum playlist_visibility_default
        string theme_preference
        string language
        timestamp created_at
        timestamp updated_at
    }
    
    GENRE {
        uuid genre_id PK
        string name UK
        text description
        uuid parent_genre_id FK
        string icon_url
        timestamp created_at
    }
    
    ALBUM {
        uuid album_id PK
        uuid artist_id FK
        string title
        text description
        string cover_art_url
        date release_date
        enum album_type
        bigint total_duration_ms
        string language
        boolean explicit
        boolean is_published
        timestamp published_at
        timestamp created_at
        timestamp updated_at
        boolean is_deleted
        timestamp deleted_at
    }
    
    TRACK {
        uuid track_id PK
        uuid artist_id FK
        uuid album_id FK
        string title
        text description
        uuid genre_id FK
        bigint duration_ms
        string isrc
        boolean explicit
        boolean is_published
        timestamp published_at
        string original_file_url
        bigint original_file_size_bytes
        string original_file_mime_type
        decimal price_vnd
        boolean allow_download
        date creation_date
        boolean can_be_shared
        timestamp created_at
        timestamp updated_at
        boolean is_deleted
        timestamp deleted_at
    }
    
    TRACKVERSION {
        uuid version_id PK
        uuid track_id FK
        enum codec
        int bitrate_kbps
        int sample_rate_hz
        string file_url
        bigint file_size_bytes
        string mime_type
        enum transcode_status
        text transcode_error
        timestamp created_at
        timestamp updated_at
        boolean is_deleted
    }
    
    PLAYLIST {
        uuid playlist_id PK
        uuid listener_id FK
        string title
        text description
        string cover_art_url
        boolean is_public
        boolean allow_collaboration
        bigint total_duration_ms
        int track_count
        timestamp created_at
        timestamp updated_at
        boolean is_deleted
        timestamp deleted_at
    }
    
    PLAYLIST_TRACK {
        uuid playlist_track_id PK
        uuid playlist_id FK
        uuid track_id FK
        int position
        uuid added_by_listener_id FK
        timestamp added_at
        boolean is_deleted
    }
    
    PLAYLISTSHARE {
        uuid share_id PK
        uuid playlist_id FK
        string share_token UK
        uuid created_by_listener_id FK
        timestamp created_at
        timestamp expires_at
        boolean is_active
    }
    
    PLAYLISTFOLLOW {
        uuid follow_id PK
        uuid listener_id FK
        uuid playlist_id FK
        timestamp followed_at
    }
    
    ARTISTFOLLOW {
        uuid follow_id PK
        uuid listener_id FK
        uuid artist_id FK
        timestamp followed_at
    }
    
    TRACKFAVORITE {
        uuid favorite_id PK
        uuid listener_id FK
        uuid track_id FK
        timestamp favorited_at
    }
    
    PURCHASE {
        uuid purchase_id PK
        uuid listener_id FK
        uuid track_id FK
        uuid album_id FK
        decimal price_vnd
        enum payment_method
        string transaction_id UK
        enum status
        timestamp purchased_at
        timestamp refunded_at
    }
    
    ROYALTYSPLIT {
        uuid split_id PK
        uuid track_id FK
        uuid album_id FK
        uuid artist_id FK
        uuid organization_id FK
        decimal percentage
        timestamp created_at
        date effective_from
        date effective_to
    }
    
    WITHDRAWAL {
        uuid withdrawal_id PK
        uuid artist_id FK
        uuid organization_id FK
        decimal amount_vnd
        string bank_account_last4
        enum status
        timestamp requested_at
        timestamp approved_at
        timestamp completed_at
        text failure_reason
    }
    
    REPORT {
        uuid report_id PK
        uuid reporter_id FK
        uuid track_id FK
        uuid album_id FK
        uuid playlist_id FK
        enum reason
        text description
        enum status
        enum severity
        text admin_notes
        enum action_taken
        timestamp reported_at
        timestamp reviewed_at
        uuid reviewed_by_admin_id FK
    }
    
    PLAYBACKEVENT {
        uuid event_id PK
        uuid listener_id FK
        uuid track_id FK
        string session_id
        timestamp played_at
        bigint duration_played_ms
        enum device_type
        string country
    }
