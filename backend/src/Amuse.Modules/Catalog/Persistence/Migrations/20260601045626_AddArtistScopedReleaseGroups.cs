using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistScopedReleaseGroups : Migration
    {
        private static readonly string[] ReleaseGroupArtistSlugColumns = ["artist_id", "slug"];
        private static readonly string[] ReleaseGroupOrganizationSlugColumns = ["organization_id", "slug"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_release_group_organization_id_slug",
                schema: "catalog",
                table: "release_group");

            migrationBuilder.AddColumn<Guid>(
                name: "artist_id",
                schema: "catalog",
                table: "release_group",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.release_group rg
                SET artist_id = (
                    SELECT r.artist_id
                    FROM catalog.release r
                    WHERE r.release_group_id = rg.id
                    ORDER BY r.created_at
                    LIMIT 1
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog.release_group
                WHERE artist_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "artist_id",
                schema: "catalog",
                table: "release_group",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_group_artist_id_slug",
                schema: "catalog",
                table: "release_group",
                columns: ReleaseGroupArtistSlugColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_group_organization_id",
                schema: "catalog",
                table: "release_group",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "FK_release_group_artist_artist_id",
                schema: "catalog",
                table: "release_group",
                column: "artist_id",
                principalSchema: "catalog",
                principalTable: "artist",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_release_group_artist_artist_id",
                schema: "catalog",
                table: "release_group");

            migrationBuilder.DropIndex(
                name: "IX_release_group_artist_id_slug",
                schema: "catalog",
                table: "release_group");

            migrationBuilder.DropIndex(
                name: "IX_release_group_organization_id",
                schema: "catalog",
                table: "release_group");

            migrationBuilder.DropColumn(
                name: "artist_id",
                schema: "catalog",
                table: "release_group");

            migrationBuilder.CreateIndex(
                name: "IX_release_group_organization_id_slug",
                schema: "catalog",
                table: "release_group",
                columns: ReleaseGroupOrganizationSlugColumns,
                unique: true);
        }
    }
}
