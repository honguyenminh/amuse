using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Listener.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileAvatarObjectKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar_object_key",
                schema: "listener",
                table: "listener_profile",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar_object_key",
                schema: "listener",
                table: "listener_profile");
        }
    }
}
