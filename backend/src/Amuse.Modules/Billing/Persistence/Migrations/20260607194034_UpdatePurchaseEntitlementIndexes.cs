using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchaseEntitlementIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_account_id_track_id",
                schema: "billing",
                table: "purchase");

            migrationBuilder.AddColumn<bool>(
                name: "is_for_sale",
                schema: "billing",
                table: "track",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "price_ceiling_minor",
                schema: "billing",
                table: "track",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "price_currency",
                schema: "billing",
                table: "track",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "price_floor_minor",
                schema: "billing",
                table: "track",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "is_for_sale",
                schema: "billing",
                table: "release",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "price_ceiling_minor",
                schema: "billing",
                table: "release",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "price_currency",
                schema: "billing",
                table: "release",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "price_floor_minor",
                schema: "billing",
                table: "release",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "royalty_split",
                schema: "billing",
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
                name: "IX_purchase_account_id_release_id",
                schema: "billing",
                table: "purchase",
                columns: new[] { "account_id", "release_id" },
                unique: true,
                filter: "release_id IS NOT NULL AND purchased_unit = 'release' AND entitlement_status = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_account_id_track_id",
                schema: "billing",
                table: "purchase",
                columns: new[] { "account_id", "track_id" },
                unique: true,
                filter: "track_id IS NOT NULL AND entitlement_status = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_royalty_split_listing_organization_id",
                schema: "billing",
                table: "royalty_split",
                column: "listing_organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_royalty_split_track_id_payee_organization_id",
                schema: "billing",
                table: "royalty_split",
                columns: new[] { "track_id", "payee_organization_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "royalty_split",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "IX_purchase_account_id_release_id",
                schema: "billing",
                table: "purchase");

            migrationBuilder.DropIndex(
                name: "IX_purchase_account_id_track_id",
                schema: "billing",
                table: "purchase");

            migrationBuilder.DropColumn(
                name: "is_for_sale",
                schema: "billing",
                table: "track");

            migrationBuilder.DropColumn(
                name: "price_ceiling_minor",
                schema: "billing",
                table: "track");

            migrationBuilder.DropColumn(
                name: "price_currency",
                schema: "billing",
                table: "track");

            migrationBuilder.DropColumn(
                name: "price_floor_minor",
                schema: "billing",
                table: "track");

            migrationBuilder.DropColumn(
                name: "is_for_sale",
                schema: "billing",
                table: "release");

            migrationBuilder.DropColumn(
                name: "price_ceiling_minor",
                schema: "billing",
                table: "release");

            migrationBuilder.DropColumn(
                name: "price_currency",
                schema: "billing",
                table: "release");

            migrationBuilder.DropColumn(
                name: "price_floor_minor",
                schema: "billing",
                table: "release");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_account_id_track_id",
                schema: "billing",
                table: "purchase",
                columns: new[] { "account_id", "track_id" },
                unique: true,
                filter: "track_id IS NOT NULL");
        }
    }
}
