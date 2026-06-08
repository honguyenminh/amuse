using System;
using Amuse.Domain.Billing;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Amuse.Modules.Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:billing.entitlement_status", "active,revoked")
                .Annotation("Npgsql:Enum:billing.entry_direction", "credit,debit")
                .Annotation("Npgsql:Enum:billing.fx_rate_source", "ecb_daily,ops_manual,stripe_quote")
                .Annotation("Npgsql:Enum:billing.journal_type", "adjustment,chargeback,hold_release,purchase,refund,stream_settlement,withdrawal")
                .Annotation("Npgsql:Enum:billing.ledger_account_type", "platform_cash,platform_revenue,psp_fee_expense,refund_liability,seller_payable_available,seller_payable_in_payout,seller_payable_pending,vat_payable")
                .Annotation("Npgsql:Enum:billing.legal_entity_type", "company,individual")
                .Annotation("Npgsql:Enum:billing.payment_status", "charged_back,free,paid,partially_refunded,pending,refunded")
                .Annotation("Npgsql:Enum:billing.payout_rail", "manual_bank,stripe_global")
                .Annotation("Npgsql:Enum:billing.payout_verification_status", "not_started,rejected,submitted,under_review,verified")
                .Annotation("Npgsql:Enum:billing.purchased_unit", "release,track")
                .Annotation("Npgsql:Enum:billing.reference_type", "adjustment,chargeback,purchase,refund,stream_settlement,withdrawal")
                .Annotation("Npgsql:Enum:billing.refund_fee_bearer", "platform,seller")
                .Annotation("Npgsql:Enum:billing.withdrawal_status", "approved,completed,failed,pending_approval,processing,requested");

            migrationBuilder.CreateTable(
                name: "account",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idp_issuer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    idp_subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    banned_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "artist",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    bio = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    aliases = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    avatar_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    cover_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    managing_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visibility_tier = table.Column<int>(type: "catalog.artist_visibility_tier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audio_master_upload_intent",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio_master_upload_intent", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audio_transcode_job",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    master_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    stream_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio_transcode_job", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    table_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    actor_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "banned_payment_instrument",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method_fingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    banned_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banned_payment_instrument", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "business_portal_profile",
                schema: "billing",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    avatar_accent_seed = table.Column<int>(type: "integer", nullable: true),
                    avatar_object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_portal_profile", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "fx_rate",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    quote_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    source = table.Column<FxRateSource>(type: "billing.fx_rate_source", nullable: false),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fx_rate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_journal",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_type = table.Column<JournalType>(type: "billing.journal_type", nullable: false),
                    reference_type = table.Column<ReferenceType>(type: "billing.reference_type", nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_journal", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_entry",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    listener_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "discovery.library_entry_kind", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "listener_preference",
                schema: "billing",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allow_unverified_artists = table.Column<bool>(type: "boolean", nullable: true),
                    set_during_onboarding = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listener_preference", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "listener_profile",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    avatar_accent_seed = table.Column<int>(type: "integer", nullable: true),
                    avatar_object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listener_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organization",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    imprint_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    org_class = table.Column<int>(type: "tenancy.org_class", nullable: false),
                    lifecycle_status = table.Column<int>(type: "tenancy.organization_lifecycle_status", nullable: false),
                    onboarding_status = table.Column<int>(type: "tenancy.organization_onboarding_status", nullable: false),
                    trust_tier = table.Column<int>(type: "tenancy.organization_trust_tier", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    approved_by_operator_id = table.Column<int>(type: "integer", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_transaction",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gross_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    payment_method_fingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<PaymentStatus>(type: "billing.payment_status", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transaction", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payout_profile",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_entity_type = table.Column<LegalEntityType>(type: "billing.legal_entity_type", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    payout_rail = table.Column<PayoutRail>(type: "billing.payout_rail", nullable: false),
                    verification_status = table.Column<PayoutVerificationStatus>(type: "billing.payout_verification_status", nullable: false),
                    external_recipient_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payout_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_operator",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claims = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_operator", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "playlist",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_listener_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    kind = table.Column<int>(type: "discovery.playlist_kind", nullable: false),
                    visibility = table.Column<int>(type: "discovery.playlist_visibility", nullable: false),
                    forked_from_playlist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "playlist_follow",
                schema: "billing",
                columns: table => new
                {
                    listener_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    followed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_follow", x => new { x.listener_profile_id, x.playlist_id });
                });

            migrationBuilder.CreateTable(
                name: "purchase",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchased_unit = table.Column<PurchasedUnit>(type: "billing.purchased_unit", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: true),
                    release_id = table.Column<Guid>(type: "uuid", nullable: true),
                    price_snapshot_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_status = table.Column<PaymentStatus>(type: "billing.payment_status", nullable: false),
                    entitlement_status = table.Column<EntitlementStatus>(type: "billing.entitlement_status", nullable: false),
                    purchased_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_allocation_snapshot",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payee_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_bps = table.Column<int>(type: "integer", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_allocation_snapshot", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_session",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_session", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seller_receivable",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_settled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller_receivable", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tax_invoice",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    gross_minor = table.Column<long>(type: "bigint", nullable: false),
                    vat_minor = table.Column<long>(type: "bigint", nullable: false),
                    net_ex_vat_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    vat_rate_bps = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_invoice", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "withdrawal_request",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<WithdrawalStatus>(type: "billing.withdrawal_status", nullable: false),
                    fx_rate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transfer_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_withdrawal_request", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "release_group",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release_group", x => x.id);
                    table.ForeignKey(
                        name: "FK_release_group_artist_artist_id",
                        column: x => x.artist_id,
                        principalSchema: "billing",
                        principalTable: "artist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entry",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_type = table.Column<LedgerAccountType>(type: "billing.ledger_account_type", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    direction = table.Column<EntryDirection>(type: "billing.entry_direction", nullable: false),
                    amount_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_entry", x => x.id);
                    table.ForeignKey(
                        name: "FK_ledger_entry_ledger_journal_journal_id",
                        column: x => x.journal_id,
                        principalSchema: "billing",
                        principalTable: "ledger_journal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_invite",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    invited_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preset_role_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    claims = table.Column<string>(type: "jsonb", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    accepted_by_account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_invite", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_invite_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "billing",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_member",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    preset_role_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    claims = table.Column<string>(type: "jsonb", nullable: false),
                    is_owner = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_member", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_member_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "billing",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "playlist_item",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_playlist_item_playlist_playlist_id",
                        column: x => x.playlist_id,
                        principalSchema: "billing",
                        principalTable: "playlist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_share_grant",
                schema: "billing",
                columns: table => new
                {
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_share_grant", x => new { x.playlist_id, x.email });
                    table.ForeignKey(
                        name: "FK_playlist_share_grant_playlist_playlist_id",
                        column: x => x.playlist_id,
                        principalSchema: "billing",
                        principalTable: "playlist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "release",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    release_type = table.Column<int>(type: "catalog.release_type", nullable: false),
                    lifecycle_status = table.Column<int>(type: "catalog.release_lifecycle_status", nullable: false),
                    release_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    upc = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    primary_genre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    language_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    label_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    p_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    c_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    original_release_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    metadata_complete = table.Column<bool>(type: "boolean", nullable: false),
                    cover_art_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release", x => x.id);
                    table.ForeignKey(
                        name: "FK_release_release_group_release_group_id",
                        column: x => x.release_group_id,
                        principalSchema: "billing",
                        principalTable: "release_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "track",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    track_number = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    explicit_flag = table.Column<bool>(type: "boolean", nullable: false),
                    isrc = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    lyrics = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    language_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    version_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    composer_credits = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    lifecycle_status = table.Column<int>(type: "catalog.track_lifecycle_status", nullable: false),
                    audio_master_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    audio_stream_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    loudness_integrated_lufs = table.Column<double>(type: "double precision", nullable: true),
                    loudness_true_peak_dbtp = table.Column<double>(type: "double precision", nullable: true),
                    loudness_range_lu = table.Column<double>(type: "double precision", nullable: true),
                    loudness_threshold_lufs = table.Column<double>(type: "double precision", nullable: true),
                    loudness_target_integrated_lufs = table.Column<double>(type: "double precision", nullable: true),
                    loudness_target_true_peak_dbtp = table.Column<double>(type: "double precision", nullable: true),
                    loudness_linear_gain_lu = table.Column<double>(type: "double precision", nullable: true),
                    loudness_analyzed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_release_release_id",
                        column: x => x.release_id,
                        principalSchema: "billing",
                        principalTable: "release",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "track_collaborator",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    role = table.Column<int>(type: "catalog.track_collaborator_role", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track_collaborator", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_collaborator_artist_artist_id",
                        column: x => x.artist_id,
                        principalSchema: "billing",
                        principalTable: "artist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_track_collaborator_track_track_id",
                        column: x => x.track_id,
                        principalSchema: "billing",
                        principalTable: "track",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "track_audio_rendition",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codec = table.Column<int>(type: "catalog.audio_codec", nullable: false),
                    bitrate_kbps = table.Column<int>(type: "integer", nullable: true),
                    sample_rate_hz = table.Column<int>(type: "integer", nullable: false),
                    bandwidth = table.Column<int>(type: "integer", nullable: false),
                    representation_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    adaptation_set_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    manifest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track_audio_rendition", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_audio_rendition_track_track_id",
                        column: x => x.track_id,
                        principalSchema: "billing",
                        principalTable: "track",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_idp_issuer_idp_subject",
                schema: "billing",
                table: "account",
                columns: new[] { "idp_issuer", "idp_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artist_managing_organization_id",
                schema: "billing",
                table: "artist",
                column: "managing_organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_artist_slug",
                schema: "billing",
                table: "artist",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audio_master_upload_intent_object_key",
                schema: "billing",
                table: "audio_master_upload_intent",
                column: "object_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audio_master_upload_intent_track_id_consumed_at",
                schema: "billing",
                table: "audio_master_upload_intent",
                columns: new[] { "track_id", "consumed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audio_transcode_job_status_created_at",
                schema: "billing",
                table: "audio_transcode_job",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audio_transcode_job_track_id",
                schema: "billing",
                table: "audio_transcode_job",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "IX_banned_payment_instrument_payment_method_fingerprint",
                schema: "billing",
                table: "banned_payment_instrument",
                column: "payment_method_fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fx_rate_base_currency_quote_currency_effective_at",
                schema: "billing",
                table: "fx_rate",
                columns: new[] { "base_currency", "quote_currency", "effective_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entry_journal_id",
                schema: "billing",
                table: "ledger_entry",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_library_entry_listener_profile_id_kind",
                schema: "billing",
                table: "library_entry",
                columns: new[] { "listener_profile_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "IX_library_entry_listener_profile_id_kind_target_id",
                schema: "billing",
                table: "library_entry",
                columns: new[] { "listener_profile_id", "kind", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_listener_profile_account_id",
                schema: "billing",
                table: "listener_profile",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_onboarding_status",
                schema: "billing",
                table: "organization",
                column: "onboarding_status");

            migrationBuilder.CreateIndex(
                name: "IX_organization_org_class",
                schema: "billing",
                table: "organization",
                column: "org_class");

            migrationBuilder.CreateIndex(
                name: "IX_organization_invite_organization_id_email_status",
                schema: "billing",
                table: "organization_invite",
                columns: new[] { "organization_id", "email", "status" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_organization_invite_token_hash",
                schema: "billing",
                table: "organization_invite",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_member_account_id",
                schema: "billing",
                table: "organization_member",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_member_organization_id_account_id",
                schema: "billing",
                table: "organization_member",
                columns: new[] { "organization_id", "account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_processed_at_created_at",
                schema: "billing",
                table: "outbox_message",
                columns: new[] { "processed_at", "created_at" },
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payout_profile_organization_id",
                schema: "billing",
                table: "payout_profile",
                column: "organization_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_operator_account_id",
                schema: "billing",
                table: "platform_operator",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_playlist_owner_liked_unique",
                schema: "billing",
                table: "playlist",
                column: "owner_listener_profile_id",
                unique: true,
                filter: "kind = 'liked'::discovery.playlist_kind");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_visibility_title",
                schema: "billing",
                table: "playlist",
                columns: new[] { "visibility", "title" });

            migrationBuilder.CreateIndex(
                name: "IX_playlist_follow_playlist_id",
                schema: "billing",
                table: "playlist_follow",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_item_playlist_id_position",
                schema: "billing",
                table: "playlist_item",
                columns: new[] { "playlist_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlist_item_playlist_id_track_id",
                schema: "billing",
                table: "playlist_item",
                columns: new[] { "playlist_id", "track_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_account_id_track_id",
                schema: "billing",
                table: "purchase",
                columns: new[] { "account_id", "track_id" },
                unique: true,
                filter: "track_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_session_account_id",
                schema: "billing",
                table: "refresh_session",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_session_token_hash",
                schema: "billing",
                table: "refresh_session",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_artist_id_slug",
                schema: "billing",
                table: "release",
                columns: new[] { "artist_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_organization_id_lifecycle_status_release_date",
                schema: "billing",
                table: "release",
                columns: new[] { "organization_id", "lifecycle_status", "release_date" });

            migrationBuilder.CreateIndex(
                name: "IX_release_release_date",
                schema: "billing",
                table: "release",
                column: "release_date");

            migrationBuilder.CreateIndex(
                name: "IX_release_release_group_id",
                schema: "billing",
                table: "release",
                column: "release_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_release_upc",
                schema: "billing",
                table: "release",
                column: "upc");

            migrationBuilder.CreateIndex(
                name: "IX_track_collaborator_artist_id",
                schema: "billing",
                table: "track_collaborator",
                column: "artist_id");

            migrationBuilder.CreateIndex(
                name: "IX_track_collaborator_track_id_artist_id",
                schema: "billing",
                table: "track_collaborator",
                columns: new[] { "track_id", "artist_id" },
                unique: true,
                filter: "artist_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_track_collaborator_track_id_display_order",
                schema: "billing",
                table: "track_collaborator",
                columns: new[] { "track_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_release_group_artist_id_slug",
                schema: "billing",
                table: "release_group",
                columns: new[] { "artist_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_group_organization_id",
                schema: "billing",
                table: "release_group",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_invoice_invoice_number",
                schema: "billing",
                table: "tax_invoice",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_track_organization_id_lifecycle_status",
                schema: "billing",
                table: "track",
                columns: new[] { "organization_id", "lifecycle_status" });

            migrationBuilder.CreateIndex(
                name: "IX_track_release_id_track_number",
                schema: "billing",
                table: "track",
                columns: new[] { "release_id", "track_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_track_audio_rendition_track_id_codec_bitrate_kbps",
                schema: "billing",
                table: "track_audio_rendition",
                columns: new[] { "track_id", "codec", "bitrate_kbps" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "audio_master_upload_intent",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "audio_transcode_job",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "banned_payment_instrument",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "business_portal_profile",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "fx_rate",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "ledger_entry",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "library_entry",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "listener_preference",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "listener_profile",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "organization_invite",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "organization_member",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payment_transaction",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payout_profile",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "platform_operator",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "playlist_follow",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "playlist_item",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "playlist_share_grant",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "purchase",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "purchase_allocation_snapshot",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "refresh_session",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "track_collaborator",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "seller_receivable",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "tax_invoice",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "track_audio_rendition",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "withdrawal_request",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "ledger_journal",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "organization",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "playlist",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "track",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "release",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "release_group",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "artist",
                schema: "billing");
        }
    }
}
