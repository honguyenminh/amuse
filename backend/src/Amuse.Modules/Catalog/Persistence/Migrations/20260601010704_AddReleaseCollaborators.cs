using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseCollaborators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .Annotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .Annotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "release_collaborator",
                schema: "catalog");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .Annotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden");
        }
    }
}
