using System;
using Amuse.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        private static readonly string[] ReleaseArtistSlugColumns = ["artist_id", "slug"];
        private static readonly string[] TrackReleaseTrackNumberColumns = ["release_id", "track_number"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.release_type", "single,ep,album,compilation");

            migrationBuilder.CreateTable(
                name: "artist",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    bio = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    avatar_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    cover_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "release",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    release_type = table.Column<ReleaseType>(type: "catalog.release_type", nullable: false),
                    release_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    cover_art_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_release", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "track",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    track_number = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    audio_master_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_track", x => x.id);
                    table.ForeignKey(
                        name: "FK_track_release_release_id",
                        column: x => x.release_id,
                        principalSchema: "catalog",
                        principalTable: "release",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_artist_slug",
                schema: "catalog",
                table: "artist",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_artist_id_slug",
                schema: "catalog",
                table: "release",
                columns: ReleaseArtistSlugColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_release_release_date",
                schema: "catalog",
                table: "release",
                column: "release_date");

            migrationBuilder.CreateIndex(
                name: "IX_track_release_id_track_number",
                schema: "catalog",
                table: "track",
                columns: TrackReleaseTrackNumberColumns,
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
                name: "release",
                schema: "catalog");
        }
    }
}
