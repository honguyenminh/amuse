using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Discovery.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LimitPlaylistDescriptionLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE discovery.playlist
                SET description = LEFT(description, 100)
                WHERE description IS NOT NULL AND char_length(description) > 100;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "discovery",
                table: "playlist",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "discovery",
                table: "playlist",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
