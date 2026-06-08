using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPayoutProfileGateB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_line1",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "address_line2",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account_last4",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account_protected",
                schema: "billing",
                table: "payout_profile",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "document_object_keys",
                schema: "billing",
                table: "payout_profile",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "representative_name",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_id_protected",
                schema: "billing",
                table: "payout_profile",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by",
                schema: "billing",
                table: "payout_profile",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payout_profile_verification_status",
                schema: "billing",
                table: "payout_profile",
                column: "verification_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payout_profile_verification_status",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "address_line1",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "address_line2",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "bank_account_last4",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "bank_account_protected",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "bank_name",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "city",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "document_object_keys",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "postal_code",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "representative_name",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "tax_id_protected",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.DropColumn(
                name: "verified_by",
                schema: "billing",
                table: "payout_profile");

            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                schema: "billing",
                table: "payout_profile",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2);
        }
    }
}
