using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioIngestHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processing_started_at",
                schema: "catalog",
                table: "audio_transcode_job",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audio_master_upload_intent",
                schema: "catalog",
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
                name: "outbox_message",
                schema: "catalog",
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

            migrationBuilder.CreateIndex(
                name: "IX_audio_master_upload_intent_object_key",
                schema: "catalog",
                table: "audio_master_upload_intent",
                column: "object_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audio_master_upload_intent_track_id_consumed_at",
                schema: "catalog",
                table: "audio_master_upload_intent",
                columns: new[] { "track_id", "consumed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_processed_at_created_at",
                schema: "catalog",
                table: "outbox_message",
                columns: new[] { "processed_at", "created_at" },
                filter: "processed_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audio_master_upload_intent",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "catalog");

            migrationBuilder.DropColumn(
                name: "processing_started_at",
                schema: "catalog",
                table: "audio_transcode_job");
        }
    }
}
