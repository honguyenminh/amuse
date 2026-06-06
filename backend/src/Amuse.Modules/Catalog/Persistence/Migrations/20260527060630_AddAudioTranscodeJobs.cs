using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioTranscodeJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation");

            migrationBuilder.AddColumn<string>(
                name: "audio_stream_key",
                schema: "catalog",
                table: "track",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audio_transcode_job",
                schema: "catalog",
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
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio_transcode_job", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audio_transcode_job_status_created_at",
                schema: "catalog",
                table: "audio_transcode_job",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audio_transcode_job_track_id",
                schema: "catalog",
                table: "audio_transcode_job",
                column: "track_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audio_transcode_job",
                schema: "catalog");

            migrationBuilder.DropColumn(
                name: "audio_stream_key",
                schema: "catalog",
                table: "track");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation");
        }
    }
}
