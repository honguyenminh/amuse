using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalProofAndFailedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "failed_at",
                schema: "billing",
                table: "withdrawal_request",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_object_key",
                schema: "billing",
                table: "withdrawal_request",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_request_organization_id",
                schema: "billing",
                table: "withdrawal_request",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_request_organization_id_status",
                schema: "billing",
                table: "withdrawal_request",
                columns: new[] { "organization_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_withdrawal_request_organization_id",
                schema: "billing",
                table: "withdrawal_request");

            migrationBuilder.DropIndex(
                name: "IX_withdrawal_request_organization_id_status",
                schema: "billing",
                table: "withdrawal_request");

            migrationBuilder.DropColumn(
                name: "failed_at",
                schema: "billing",
                table: "withdrawal_request");

            migrationBuilder.DropColumn(
                name: "proof_object_key",
                schema: "billing",
                table: "withdrawal_request");
        }
    }
}
