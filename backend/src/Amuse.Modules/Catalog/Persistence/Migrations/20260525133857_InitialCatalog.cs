using System;
using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation");

            migrationBuilder.CreateTable(
                name: "album",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    release_type = table.Column<ReleaseType>(type: "catalog.release_type", nullable: false),
                    release_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    cover_art_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_album", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "artist",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    bio = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    cover_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "track",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    album_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    track_number = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    audio_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_album_album_id",
                        column: x => x.album_id,
                        principalSchema: "catalog",
                        principalTable: "album",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_album_artist_id_slug",
                schema: "catalog",
                table: "album",
                columns: new[] { "artist_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_album_release_date",
                schema: "catalog",
                table: "album",
                column: "release_date");

            migrationBuilder.CreateIndex(
                name: "IX_artist_slug",
                schema: "catalog",
                table: "artist",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_track_album_id_track_number",
                schema: "catalog",
                table: "track",
                columns: new[] { "album_id", "track_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artist",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "track",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "album",
                schema: "catalog");
        }
    }
}
