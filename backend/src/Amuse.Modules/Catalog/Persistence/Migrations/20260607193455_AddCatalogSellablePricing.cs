using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogSellablePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_for_sale",
                schema: "catalog",
                table: "track",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "price_ceiling_minor",
                schema: "catalog",
                table: "track",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "price_currency",
                schema: "catalog",
                table: "track",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "price_floor_minor",
                schema: "catalog",
                table: "track",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "is_for_sale",
                schema: "catalog",
                table: "release",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "price_ceiling_minor",
                schema: "catalog",
                table: "release",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "price_currency",
                schema: "catalog",
                table: "release",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "price_floor_minor",
                schema: "catalog",
                table: "release",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "royalty_split",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payee_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_bps = table.Column<int>(type: "integer", nullable: false),
                    listing_organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_royalty_split", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_royalty_split_listing_organization_id",
                schema: "catalog",
                table: "royalty_split",
                column: "listing_organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_royalty_split_track_id_payee_organization_id",
                schema: "catalog",
                table: "royalty_split",
                columns: new[] { "track_id", "payee_organization_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "royalty_split",
                schema: "catalog");

            migrationBuilder.DropColumn(
                name: "is_for_sale",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "price_ceiling_minor",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "price_currency",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "price_floor_minor",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "is_for_sale",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "price_ceiling_minor",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "price_currency",
                schema: "catalog",
                table: "release");

            migrationBuilder.DropColumn(
                name: "price_floor_minor",
                schema: "catalog",
                table: "release");
        }
    }
}
