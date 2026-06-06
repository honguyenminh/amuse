using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogMetadataMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "composer_credits",
                schema: "catalog",
                table: "track",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "isrc",
                schema: "catalog",
                table: "track",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "language_code",
                schema: "catalog",
                table: "track",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lyrics",
                schema: "catalog",
                table: "track",
                type: "character varying(12000)",
                maxLength: 12000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "version_title",
                schema: "catalog",
                table: "track",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "c_line",
                schema: "catalog",
                table: "release",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "catalog",
                table: "release",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "label_name",
                schema: "catalog",
                table: "release",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "language_code",
                schema: "catalog",
                table: "release",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "metadata_complete",
                schema: "catalog",
                table: "release",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "original_release_date",
                schema: "catalog",
                table: "release",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "p_line",
                schema: "catalog",
                table: "release",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_genre",
                schema: "catalog",
                table: "release",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tags",
                schema: "catalog",
                table: "release",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "upc",
                schema: "catalog",
                table: "release",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aliases",
                schema: "catalog",
                table: "artist",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                schema: "catalog",
                table: "artist",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website_url",
                schema: "catalog",
                table: "artist",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_upc",
                schema: "catalog",
                table: "release",
                column: "upc");

            migrationBuilder.AddForeignKey(
                name: "FK_release_release_group_release_group_id",
                schema: "catalog",
                table: "release",
                column: "release_group_id",
                principalSchema: "catalog",
                principalTable: "release_group",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_release_release_group_release_group_id",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropIndex(
                name: "IX_release_upc",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "composer_credits",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "isrc",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "language_code",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "lyrics",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "version_title",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "c_line",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "label_name",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "language_code",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "metadata_complete",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "original_release_date",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "p_line",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "primary_genre",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "tags",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "upc",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "aliases",
                schema: "catalog",
                table: "artist");

            migrationBuilder.DropColumn(
                name: "country_code",
                schema: "catalog",
                table: "artist");

            migrationBuilder.DropColumn(
                name: "website_url",
                schema: "catalog",
                table: "artist");
        }
    }
}
