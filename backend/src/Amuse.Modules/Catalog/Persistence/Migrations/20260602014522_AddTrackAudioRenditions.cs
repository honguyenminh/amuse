using System;
using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackAudioRenditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .Annotation("Npgsql:Enum:catalog.audio_codec", "flac,opus,aac")
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .Annotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .Annotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden");

            migrationBuilder.CreateTable(
                name: "track_audio_rendition",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codec = table.Column<AudioCodec>(type: "catalog.audio_codec", nullable: false),
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
                        principalSchema: "catalog",
                        principalTable: "track",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_track_audio_rendition_track_id_codec_bitrate_kbps",
                schema: "catalog",
                table: "track_audio_rendition",
                columns: new[] { "track_id", "codec", "bitrate_kbps" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "track_audio_rendition",
                schema: "catalog");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .Annotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .Annotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .OldAnnotation("Npgsql:Enum:catalog.audio_codec", "flac,opus,aac")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden");
        }
    }
}
