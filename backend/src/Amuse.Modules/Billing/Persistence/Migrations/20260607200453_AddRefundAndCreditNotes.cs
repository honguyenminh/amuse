using System;
using Amuse.Domain.Billing;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundAndCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:billing.entitlement_status", "active,revoked")
                .Annotation("Npgsql:Enum:billing.entry_direction", "credit,debit")
                .Annotation("Npgsql:Enum:billing.fx_rate_source", "ecb_daily,ops_manual,stripe_quote")
                .Annotation("Npgsql:Enum:billing.journal_type", "adjustment,chargeback,hold_release,purchase,refund,stream_settlement,withdrawal")
                .Annotation("Npgsql:Enum:billing.ledger_account_type", "platform_cash,platform_revenue,psp_fee_expense,refund_liability,seller_payable_available,seller_payable_in_payout,seller_payable_pending,vat_payable")
                .Annotation("Npgsql:Enum:billing.legal_entity_type", "company,individual")
                .Annotation("Npgsql:Enum:billing.payment_status", "charged_back,free,paid,partially_refunded,pending,refunded")
                .Annotation("Npgsql:Enum:billing.payout_rail", "manual_bank,stripe_global")
                .Annotation("Npgsql:Enum:billing.payout_verification_status", "not_started,rejected,submitted,under_review,verified")
                .Annotation("Npgsql:Enum:billing.purchased_unit", "release,track")
                .Annotation("Npgsql:Enum:billing.reference_type", "adjustment,chargeback,purchase,refund,stream_settlement,withdrawal")
                .Annotation("Npgsql:Enum:billing.refund_fee_bearer", "platform,seller")
                .Annotation("Npgsql:Enum:billing.refund_initiator_role", "platform,seller")
                .Annotation("Npgsql:Enum:billing.withdrawal_status", "approved,completed,failed,pending_approval,processing,requested")
                .OldAnnotation("Npgsql:Enum:billing.entitlement_status", "active,revoked")
                .OldAnnotation("Npgsql:Enum:billing.entry_direction", "credit,debit")
                .OldAnnotation("Npgsql:Enum:billing.fx_rate_source", "ecb_daily,ops_manual,stripe_quote")
                .OldAnnotation("Npgsql:Enum:billing.journal_type", "adjustment,chargeback,hold_release,purchase,refund,stream_settlement,withdrawal")
                .OldAnnotation("Npgsql:Enum:billing.ledger_account_type", "platform_cash,platform_revenue,psp_fee_expense,refund_liability,seller_payable_available,seller_payable_in_payout,seller_payable_pending,vat_payable")
                .OldAnnotation("Npgsql:Enum:billing.legal_entity_type", "company,individual")
                .OldAnnotation("Npgsql:Enum:billing.payment_status", "charged_back,free,paid,partially_refunded,pending,refunded")
                .OldAnnotation("Npgsql:Enum:billing.payout_rail", "manual_bank,stripe_global")
                .OldAnnotation("Npgsql:Enum:billing.payout_verification_status", "not_started,rejected,submitted,under_review,verified")
                .OldAnnotation("Npgsql:Enum:billing.purchased_unit", "release,track")
                .OldAnnotation("Npgsql:Enum:billing.reference_type", "adjustment,chargeback,purchase,refund,stream_settlement,withdrawal")
                .OldAnnotation("Npgsql:Enum:billing.refund_fee_bearer", "platform,seller")
                .OldAnnotation("Npgsql:Enum:billing.withdrawal_status", "approved,completed,failed,pending_approval,processing,requested");

            migrationBuilder.AddColumn<RefundFeeBearer>(
                name: "refund_fee_bearer",
                schema: "billing",
                table: "purchase",
                type: "billing.refund_fee_bearer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "refund_initiated_by_account_id",
                schema: "billing",
                table: "purchase",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<RefundInitiatorRole>(
                name: "refund_initiator_role",
                schema: "billing",
                table: "purchase",
                type: "billing.refund_initiator_role",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refund_reason",
                schema: "billing",
                table: "purchase",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "refund_requested_at",
                schema: "billing",
                table: "purchase",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "refunded_at",
                schema: "billing",
                table: "purchase",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "credit_note",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_note_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tax_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    gross_minor = table.Column<long>(type: "bigint", nullable: false),
                    vat_minor = table.Column<long>(type: "bigint", nullable: false),
                    net_ex_vat_minor = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    vat_rate_bps = table.Column<int>(type: "integer", nullable: false),
                    refund_fee_bearer = table.Column<RefundFeeBearer>(type: "billing.refund_fee_bearer", nullable: false),
                    refund_fee_minor = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_note", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_credit_note_number",
                schema: "billing",
                table: "credit_note",
                column: "credit_note_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_purchase_id",
                schema: "billing",
                table: "credit_note",
                column: "purchase_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_note",
                schema: "billing");

            migrationBuilder.DropColumn(
                name: "refund_fee_bearer",
                schema: "billing",
                table: "purchase");

            migrationBuilder.DropColumn(
                name: "refund_initiated_by_account_id",
                schema: "billing",
                table: "purchase");

            migrationBuilder.DropColumn(
                name: "refund_initiator_role",
                schema: "billing",
                table: "purchase");

            migrationBuilder.DropColumn(
                name: "refund_reason",
                schema: "billing",
                table: "purchase");

            migrationBuilder.DropColumn(
                name: "refund_requested_at",
                schema: "billing",
                table: "purchase");

            migrationBuilder.DropColumn(
                name: "refunded_at",
                schema: "billing",
                table: "purchase");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:billing.entitlement_status", "active,revoked")
                .Annotation("Npgsql:Enum:billing.entry_direction", "credit,debit")
                .Annotation("Npgsql:Enum:billing.fx_rate_source", "ecb_daily,ops_manual,stripe_quote")
                .Annotation("Npgsql:Enum:billing.journal_type", "adjustment,chargeback,hold_release,purchase,refund,stream_settlement,withdrawal")
                .Annotation("Npgsql:Enum:billing.ledger_account_type", "platform_cash,platform_revenue,psp_fee_expense,refund_liability,seller_payable_available,seller_payable_in_payout,seller_payable_pending,vat_payable")
                .Annotation("Npgsql:Enum:billing.legal_entity_type", "company,individual")
                .Annotation("Npgsql:Enum:billing.payment_status", "charged_back,free,paid,partially_refunded,pending,refunded")
                .Annotation("Npgsql:Enum:billing.payout_rail", "manual_bank,stripe_global")
                .Annotation("Npgsql:Enum:billing.payout_verification_status", "not_started,rejected,submitted,under_review,verified")
                .Annotation("Npgsql:Enum:billing.purchased_unit", "release,track")
                .Annotation("Npgsql:Enum:billing.reference_type", "adjustment,chargeback,purchase,refund,stream_settlement,withdrawal")
                .Annotation("Npgsql:Enum:billing.refund_fee_bearer", "platform,seller")
                .Annotation("Npgsql:Enum:billing.withdrawal_status", "approved,completed,failed,pending_approval,processing,requested")
                .OldAnnotation("Npgsql:Enum:billing.entitlement_status", "active,revoked")
                .OldAnnotation("Npgsql:Enum:billing.entry_direction", "credit,debit")
                .OldAnnotation("Npgsql:Enum:billing.fx_rate_source", "ecb_daily,ops_manual,stripe_quote")
                .OldAnnotation("Npgsql:Enum:billing.journal_type", "adjustment,chargeback,hold_release,purchase,refund,stream_settlement,withdrawal")
                .OldAnnotation("Npgsql:Enum:billing.ledger_account_type", "platform_cash,platform_revenue,psp_fee_expense,refund_liability,seller_payable_available,seller_payable_in_payout,seller_payable_pending,vat_payable")
                .OldAnnotation("Npgsql:Enum:billing.legal_entity_type", "company,individual")
                .OldAnnotation("Npgsql:Enum:billing.payment_status", "charged_back,free,paid,partially_refunded,pending,refunded")
                .OldAnnotation("Npgsql:Enum:billing.payout_rail", "manual_bank,stripe_global")
                .OldAnnotation("Npgsql:Enum:billing.payout_verification_status", "not_started,rejected,submitted,under_review,verified")
                .OldAnnotation("Npgsql:Enum:billing.purchased_unit", "release,track")
                .OldAnnotation("Npgsql:Enum:billing.reference_type", "adjustment,chargeback,purchase,refund,stream_settlement,withdrawal")
                .OldAnnotation("Npgsql:Enum:billing.refund_fee_bearer", "platform,seller")
                .OldAnnotation("Npgsql:Enum:billing.refund_initiator_role", "platform,seller")
                .OldAnnotation("Npgsql:Enum:billing.withdrawal_status", "approved,completed,failed,pending_approval,processing,requested");
        }
    }
}
