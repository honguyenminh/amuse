using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Tenancy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationInvitesAndMigrateClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_invite",
                schema: "tenancy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    invited_by_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preset_role_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    claims = table.Column<string>(type: "jsonb", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    accepted_by_account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_invite", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_invite_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "tenancy",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_invite_organization_id_email_status",
                schema: "tenancy",
                table: "organization_invite",
                columns: new[] { "organization_id", "email", "status" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_organization_invite_token_hash",
                schema: "tenancy",
                table: "organization_invite",
                column: "token_hash",
                unique: true);

            migrationBuilder.Sql(
                """
                UPDATE tenancy.organization_member AS m
                SET claims = (
                    SELECT COALESCE(jsonb_agg(to_jsonb(
                        CASE value
                            WHEN 'org:read' THEN 'read:org:all'
                            WHEN 'org:manage' THEN 'manage:org:all'
                            WHEN 'membership:read' THEN 'read:membership:all'
                            WHEN 'membership:manage' THEN 'manage:membership:all'
                            WHEN 'catalog:read' THEN 'read:catalog:all'
                            WHEN 'catalog:upload' THEN 'upload:catalog:all'
                            WHEN 'catalog:write_draft' THEN 'write_draft:catalog:all'
                            WHEN 'catalog:publish_public' THEN 'publish_public:catalog:all'
                            WHEN 'payout:read' THEN 'read:payout:all'
                            ELSE value
                        END
                    ) ORDER BY 1), '[]'::jsonb)
                    FROM jsonb_array_elements_text(m.claims) AS elem(value)
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_invite",
                schema: "tenancy");
        }
    }
}
