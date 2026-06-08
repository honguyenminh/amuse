using System;
using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCollaboratorsToTrack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "release_collaborator",
                schema: "catalog");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .Annotation("Npgsql:Enum:catalog.audio_codec", "flac,opus,aac")
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .Annotation("Npgsql:Enum:catalog.track_collaborator_role", "featured")
                .Annotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .OldAnnotation("Npgsql:Enum:catalog.audio_codec", "flac,opus,aac")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden");

            migrationBuilder.CreateTable(
                name: "track_collaborator",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    role = table.Column<TrackCollaboratorRole>(type: "catalog.track_collaborator_role", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track_collaborator", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_collaborator_artist_artist_id",
                        column: x => x.artist_id,
                        principalSchema: "catalog",
                        principalTable: "artist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_track_collaborator_track_track_id",
                        column: x => x.track_id,
                        principalSchema: "catalog",
                        principalTable: "track",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_track_collaborator_artist_id",
                schema: "catalog",
                table: "track_collaborator",
                column: "artist_id");

            migrationBuilder.CreateIndex(
                name: "IX_track_collaborator_track_id_artist_id",
                schema: "catalog",
                table: "track_collaborator",
                columns: new[] { "track_id", "artist_id" },
                unique: true,
                filter: "artist_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_track_collaborator_track_id_display_order",
                schema: "catalog",
                table: "track_collaborator",
                columns: new[] { "track_id", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "track_collaborator",
                schema: "catalog");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .Annotation("Npgsql:Enum:catalog.audio_codec", "flac,opus,aac")
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .Annotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .Annotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .OldAnnotation("Npgsql:Enum:catalog.audio_codec", "flac,opus,aac")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.track_collaborator_role", "featured")
                .OldAnnotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden");

            migrationBuilder.CreateTable(
                name: "release_collaborator",
                schema: "catalog",
                columns: table => new
                {
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "catalog.release_collaborator_role", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release_collaborator", x => new { x.release_id, x.artist_id, x.role });
                    table.ForeignKey(
                        name: "FK_release_collaborator_artist_artist_id",
                        column: x => x.artist_id,
                        principalSchema: "catalog",
                        principalTable: "artist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_release_collaborator_release_release_id",
                        column: x => x.release_id,
                        principalSchema: "catalog",
                        principalTable: "release",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_release_collaborator_artist_id",
                schema: "catalog",
                table: "release_collaborator",
                column: "artist_id");

            migrationBuilder.CreateIndex(
                name: "IX_release_collaborator_release_id_display_order",
                schema: "catalog",
                table: "release_collaborator",
                columns: new[] { "release_id", "display_order" });
        }
    }
}
