using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledReleaseLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .Annotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .Annotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                .OldAnnotation("Npgsql:Enum:catalog.release_collaborator_role", "featured")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived,scheduled")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden");
        }
    }
}
