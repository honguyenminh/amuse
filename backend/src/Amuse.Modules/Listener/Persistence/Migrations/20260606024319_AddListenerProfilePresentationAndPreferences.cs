using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Listener.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListenerProfilePresentationAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "avatar_accent_seed",
                schema: "listener",
                table: "listener_profile",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "listener",
                table: "listener_profile",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "listener",
                table: "listener_profile",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "listener_preference",
                schema: "listener",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allow_unverified_artists = table.Column<bool>(type: "boolean", nullable: true),
                    set_during_onboarding = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listener_preference", x => x.account_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listener_preference",
                schema: "listener");

            migrationBuilder.DropColumn(
                name: "avatar_accent_seed",
                schema: "listener",
                table: "listener_profile");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "listener",
                table: "listener_profile");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "listener",
                table: "listener_profile");
        }
    }
}
