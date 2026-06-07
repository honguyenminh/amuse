using System;
using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogManagementModel : Migration
    {
        private static readonly string[] TrackOrganizationLifecycleStatusColumns = ["organization_id", "lifecycle_status"];
        private static readonly string[] ReleaseOrganizationLifecycleStatusReleaseDateColumns = ["organization_id", "lifecycle_status", "release_date"];
        private static readonly string[] ReleaseGroupOrganizationSlugColumns = ["organization_id", "slug"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .Annotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation");

            migrationBuilder.AddColumn<bool>(
                name: "explicit_flag",
                schema: "catalog",
                table: "track",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TrackLifecycleStatus>(
                name: "lifecycle_status",
                schema: "catalog",
                table: "track",
                type: "catalog.track_lifecycle_status",
                nullable: false,
                defaultValue: TrackLifecycleStatus.Published);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                schema: "catalog",
                table: "track",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Parse("019e5000-0000-7000-8000-000000000099"));

            migrationBuilder.AddColumn<ReleaseLifecycleStatus>(
                name: "lifecycle_status",
                schema: "catalog",
                table: "release",
                type: "catalog.release_lifecycle_status",
                nullable: false,
                defaultValue: ReleaseLifecycleStatus.Published);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                schema: "catalog",
                table: "release",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Parse("019e5000-0000-7000-8000-000000000099"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                schema: "catalog",
                table: "release",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "release_group_id",
                schema: "catalog",
                table: "release",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "catalog",
                table: "release",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "managing_organization_id",
                schema: "catalog",
                table: "artist",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<ArtistVisibilityTier>(
                name: "visibility_tier",
                schema: "catalog",
                table: "artist",
                type: "catalog.artist_visibility_tier",
                nullable: false,
                defaultValue: ArtistVisibilityTier.PlatformVerified);

            migrationBuilder.CreateTable(
                name: "release_group",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release_group", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_track_organization_id_lifecycle_status",
                schema: "catalog",
                table: "track",
                columns: TrackOrganizationLifecycleStatusColumns);

            migrationBuilder.CreateIndex(
                name: "IX_release_organization_id_lifecycle_status_release_date",
                schema: "catalog",
                table: "release",
                columns: ReleaseOrganizationLifecycleStatusReleaseDateColumns);

            migrationBuilder.CreateIndex(
                name: "IX_release_release_group_id",
                schema: "catalog",
                table: "release",
                column: "release_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_artist_managing_organization_id",
                schema: "catalog",
                table: "artist",
                column: "managing_organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_release_group_organization_id_slug",
                schema: "catalog",
                table: "release_group",
                columns: ReleaseGroupOrganizationSlugColumns,
                unique: true);

            migrationBuilder.Sql("""
                UPDATE catalog.release
                SET published_at = created_at,
                    updated_at = created_at
                WHERE lifecycle_status = 'published'::catalog.release_lifecycle_status
                  AND published_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "release_group",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_track_organization_id_lifecycle_status",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropIndex(
                name: "IX_release_organization_id_lifecycle_status_release_date",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropIndex(
                name: "IX_release_release_group_id",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropIndex(
                name: "IX_artist_managing_organization_id",
                schema: "catalog",
                table: "artist");

            migrationBuilder.DropColumn(
                name: "explicit_flag",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "lifecycle_status",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "organization_id",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "lifecycle_status",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "organization_id",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "published_at",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "release_group_id",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "managing_organization_id",
                schema: "catalog",
                table: "artist");

            migrationBuilder.DropColumn(
                name: "visibility_tier",
                schema: "catalog",
                table: "artist");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.artist_visibility_tier", "unverified,platform_verified")
                .OldAnnotation("Npgsql:Enum:catalog.audio_transcode_job_status", "queued,processing,succeeded,failed")
                .OldAnnotation("Npgsql:Enum:catalog.release_lifecycle_status", "draft,processing,ready,published,hidden,archived")
                .OldAnnotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation")
                .OldAnnotation("Npgsql:Enum:catalog.track_lifecycle_status", "draft,processing,ready,published,hidden");
        }
    }
}
