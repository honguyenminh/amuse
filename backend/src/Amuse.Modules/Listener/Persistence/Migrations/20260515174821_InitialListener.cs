using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Listener.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialListener : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "listener");

            migrationBuilder.CreateTable(
                name: "listener_profile",
                schema: "listener",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listener_profile", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listener_profile_account_id",
                schema: "listener",
                table: "listener_profile",
                column: "account_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listener_profile",
                schema: "listener");
        }
    }
}
