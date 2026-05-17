using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentitySessionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS identity.audit_log CASCADE;
                DROP TABLE IF EXISTS identity.listener_profile CASCADE;
                DROP TABLE IF EXISTS identity.organization_member CASCADE;
                DROP TABLE IF EXISTS identity.platform_operator CASCADE;
                """);

            migrationBuilder.CreateTable(
                name: "refresh_session",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_session", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "token_blacklist",
                schema: "identity",
                columns: table => new
                {
                    jti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token_blacklist", x => x.jti);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_session_account_id",
                schema: "identity",
                table: "refresh_session",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_session_token_hash",
                schema: "identity",
                table: "refresh_session",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "token_blacklist",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "refresh_session",
                schema: "identity");
        }
    }
}
