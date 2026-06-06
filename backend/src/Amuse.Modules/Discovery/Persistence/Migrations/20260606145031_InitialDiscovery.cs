using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Discovery.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "discovery");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discovery.library_entry_kind", "saved_playlist,saved_release")
                .Annotation("Npgsql:Enum:discovery.playlist_visibility", "private,public");

            migrationBuilder.CreateTable(
                name: "library_entry",
                schema: "discovery",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    listener_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "discovery.library_entry_kind", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "liked_track",
                schema: "discovery",
                columns: table => new
                {
                    listener_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    liked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liked_track", x => new { x.listener_profile_id, x.track_id });
                });

            migrationBuilder.CreateTable(
                name: "playlist",
                schema: "discovery",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_listener_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    visibility = table.Column<int>(type: "discovery.playlist_visibility", nullable: false),
                    forked_from_playlist_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "playlist_follow",
                schema: "discovery",
                columns: table => new
                {
                    listener_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    followed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_follow", x => new { x.listener_profile_id, x.playlist_id });
                });

            migrationBuilder.CreateTable(
                name: "playlist_item",
                schema: "discovery",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_playlist_item_playlist_playlist_id",
                        column: x => x.playlist_id,
                        principalSchema: "discovery",
                        principalTable: "playlist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "playlist_share_grant",
                schema: "discovery",
                columns: table => new
                {
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    playlist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_playlist_share_grant", x => new { x.playlist_id, x.email });
                    table.ForeignKey(
                        name: "FK_playlist_share_grant_playlist_playlist_id",
                        column: x => x.playlist_id,
                        principalSchema: "discovery",
                        principalTable: "playlist",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_library_entry_listener_profile_id_kind",
                schema: "discovery",
                table: "library_entry",
                columns: new[] { "listener_profile_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "IX_library_entry_listener_profile_id_kind_target_id",
                schema: "discovery",
                table: "library_entry",
                columns: new[] { "listener_profile_id", "kind", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlist_owner_listener_profile_id",
                schema: "discovery",
                table: "playlist",
                column: "owner_listener_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_visibility_title",
                schema: "discovery",
                table: "playlist",
                columns: new[] { "visibility", "title" });

            migrationBuilder.CreateIndex(
                name: "IX_playlist_follow_playlist_id",
                schema: "discovery",
                table: "playlist_follow",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_item_playlist_id_position",
                schema: "discovery",
                table: "playlist_item",
                columns: new[] { "playlist_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_playlist_item_playlist_id_track_id",
                schema: "discovery",
                table: "playlist_item",
                columns: new[] { "playlist_id", "track_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "library_entry",
                schema: "discovery");

            migrationBuilder.DropTable(
                name: "liked_track",
                schema: "discovery");

            migrationBuilder.DropTable(
                name: "playlist_follow",
                schema: "discovery");

            migrationBuilder.DropTable(
                name: "playlist_item",
                schema: "discovery");

            migrationBuilder.DropTable(
                name: "playlist_share_grant",
                schema: "discovery");

            migrationBuilder.DropTable(
                name: "playlist",
                schema: "discovery");
        }
    }
}
