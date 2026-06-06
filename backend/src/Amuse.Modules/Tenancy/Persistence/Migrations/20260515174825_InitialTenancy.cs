using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Tenancy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenancy");

            migrationBuilder.CreateTable(
                name: "organization_member",
                schema: "tenancy",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    preset_role_label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    claims = table.Column<string>(type: "jsonb", nullable: false),
                    is_owner = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_member", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_member_account_id",
                schema: "tenancy",
                table: "organization_member",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_member_organization_id_account_id",
                schema: "tenancy",
                table: "organization_member",
                columns: new[] { "organization_id", "account_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_member",
                schema: "tenancy");
        }
    }
}
