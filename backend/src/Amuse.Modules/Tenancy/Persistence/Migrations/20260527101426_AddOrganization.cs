using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Tenancy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:tenancy.org_class", "indie_group,backing_org")
                .Annotation("Npgsql:Enum:tenancy.organization_lifecycle_status", "draft,active,suspended,closed")
                .Annotation("Npgsql:Enum:tenancy.organization_onboarding_status", "not_required,pending_review,approved,rejected")
                .Annotation("Npgsql:Enum:tenancy.organization_trust_tier", "unverified,identity_verified,platform_verified");

            migrationBuilder.CreateTable(
                name: "organization",
                schema: "tenancy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    org_class = table.Column<int>(type: "tenancy.org_class", nullable: false),
                    lifecycle_status = table.Column<int>(type: "tenancy.organization_lifecycle_status", nullable: false),
                    onboarding_status = table.Column<int>(type: "tenancy.organization_onboarding_status", nullable: false),
                    trust_tier = table.Column<int>(type: "tenancy.organization_trust_tier", nullable: false),
                    created_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    approved_by_operator_id = table.Column<int>(type: "integer", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_onboarding_status",
                schema: "tenancy",
                table: "organization",
                column: "onboarding_status");

            migrationBuilder.CreateIndex(
                name: "IX_organization_org_class",
                schema: "tenancy",
                table: "organization",
                column: "org_class");

            migrationBuilder.AddForeignKey(
                name: "FK_organization_member_organization_organization_id",
                schema: "tenancy",
                table: "organization_member",
                column: "organization_id",
                principalSchema: "tenancy",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_organization_member_organization_organization_id",
                schema: "tenancy",
                table: "organization_member");

            migrationBuilder.DropTable(
                name: "organization",
                schema: "tenancy");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:tenancy.org_class", "indie_group,backing_org")
                .OldAnnotation("Npgsql:Enum:tenancy.organization_lifecycle_status", "draft,active,suspended,closed")
                .OldAnnotation("Npgsql:Enum:tenancy.organization_onboarding_status", "not_required,pending_review,approved,rejected")
                .OldAnnotation("Npgsql:Enum:tenancy.organization_trust_tier", "unverified,identity_verified,platform_verified");
        }
    }
}
