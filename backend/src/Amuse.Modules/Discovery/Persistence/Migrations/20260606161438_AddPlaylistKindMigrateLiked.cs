using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Discovery.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistKindMigrateLiked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_playlist_owner_listener_profile_id",
                schema: "discovery",
                table: "playlist");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discovery.library_entry_kind", "saved_playlist,saved_release")
                .Annotation("Npgsql:Enum:discovery.playlist_kind", "user,liked")
                .Annotation("Npgsql:Enum:discovery.playlist_visibility", "private,public")
                .OldAnnotation("Npgsql:Enum:discovery.library_entry_kind", "saved_playlist,saved_release")
                .OldAnnotation("Npgsql:Enum:discovery.playlist_visibility", "private,public");

            migrationBuilder.AddColumn<int>(
                name: "kind",
                schema: "discovery",
                table: "playlist",
                type: "discovery.playlist_kind",
                nullable: false,
                defaultValueSql: "'user'::discovery.playlist_kind");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_owner_listener_profile_id",
                schema: "discovery",
                table: "playlist",
                column: "owner_listener_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_playlist_owner_liked_unique",
                schema: "discovery",
                table: "playlist",
                column: "owner_listener_profile_id",
                unique: true,
                filter: "kind = 'liked'::discovery.playlist_kind");

            migrationBuilder.Sql(
                """
                WITH inserted_playlists AS (
                    INSERT INTO discovery.playlist (
                        id,
                        owner_listener_profile_id,
                        title,
                        description,
                        kind,
                        visibility,
                        forked_from_playlist_id,
                        created_at,
                        updated_at)
                    SELECT
                        gen_random_uuid(),
                        listener_profile_id,
                        'Liked',
                        NULL,
                        'liked'::discovery.playlist_kind,
                        'private'::discovery.playlist_visibility,
                        NULL,
                        MIN(liked_at),
                        MAX(liked_at)
                    FROM discovery.liked_track
                    GROUP BY listener_profile_id
                    RETURNING id, owner_listener_profile_id),
                ranked_likes AS (
                    SELECT
                        p.id AS playlist_id,
                        lt.track_id,
                        lt.liked_at,
                        ROW_NUMBER() OVER (
                            PARTITION BY lt.listener_profile_id
                            ORDER BY lt.liked_at DESC) AS position
                    FROM discovery.liked_track lt
                    INNER JOIN inserted_playlists p
                        ON p.owner_listener_profile_id = lt.listener_profile_id)
                INSERT INTO discovery.playlist_item (
                    id,
                    playlist_id,
                    track_id,
                    position,
                    added_at)
                SELECT
                    gen_random_uuid(),
                    playlist_id,
                    track_id,
                    position,
                    liked_at
                FROM ranked_likes;
                """);

            migrationBuilder.DropTable(
                name: "liked_track",
                schema: "discovery");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_playlist_owner_liked_unique",
                schema: "discovery",
                table: "playlist");

            migrationBuilder.DropIndex(
                name: "IX_playlist_owner_listener_profile_id",
                schema: "discovery",
                table: "playlist");

            migrationBuilder.CreateTable(
                name: "liked_track",
                schema: "discovery",
                columns: table => new
                {
                    listener_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    liked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liked_track", x => new { x.listener_profile_id, x.track_id });
                });

            migrationBuilder.DropColumn(
                name: "kind",
                schema: "discovery",
                table: "playlist");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:discovery.library_entry_kind", "saved_playlist,saved_release")
                .Annotation("Npgsql:Enum:discovery.playlist_visibility", "private,public")
                .OldAnnotation("Npgsql:Enum:discovery.library_entry_kind", "saved_playlist,saved_release")
                .OldAnnotation("Npgsql:Enum:discovery.playlist_kind", "user,liked")
                .OldAnnotation("Npgsql:Enum:discovery.playlist_visibility", "private,public");

            migrationBuilder.CreateIndex(
                name: "IX_playlist_owner_listener_profile_id",
                schema: "discovery",
                table: "playlist",
                column: "owner_listener_profile_id");
        }
    }
}
