using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionCheckoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "checkout_session_id",
                schema: "billing",
                table: "payment_transaction",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "psp_fee_minor",
                schema: "billing",
                table: "payment_transaction",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_transaction_checkout_session_id",
                schema: "billing",
                table: "payment_transaction",
                column: "checkout_session_id",
                unique: true,
                filter: "checkout_session_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payment_transaction_checkout_session_id",
                schema: "billing",
                table: "payment_transaction");

            migrationBuilder.DropColumn(
                name: "checkout_session_id",
                schema: "billing",
                table: "payment_transaction");

            migrationBuilder.DropColumn(
                name: "psp_fee_minor",
                schema: "billing",
                table: "payment_transaction");
        }
    }
}
