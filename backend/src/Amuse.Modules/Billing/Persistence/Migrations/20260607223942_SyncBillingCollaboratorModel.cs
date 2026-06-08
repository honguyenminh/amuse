using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Billing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncBillingCollaboratorModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Upgrade path: DBs that applied InitialBilling before track-level collaborators.
            // Fresh installs already create billing.track_collaborator in InitialBilling.
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS billing.release_collaborator CASCADE;

                CREATE TABLE IF NOT EXISTS billing.track_collaborator (
                    id uuid NOT NULL,
                    track_id uuid NOT NULL,
                    artist_id uuid NULL,
                    display_name character varying(300) NULL,
                    role catalog.track_collaborator_role NOT NULL,
                    display_order integer NOT NULL,
                    CONSTRAINT "PK_track_collaborator" PRIMARY KEY (id),
                    CONSTRAINT "FK_track_collaborator_artist_artist_id" FOREIGN KEY (artist_id)
                        REFERENCES billing.artist (id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_track_collaborator_track_track_id" FOREIGN KEY (track_id)
                        REFERENCES billing.track (id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_track_collaborator_artist_id"
                    ON billing.track_collaborator (artist_id);

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_track_collaborator_track_id_artist_id"
                    ON billing.track_collaborator (track_id, artist_id)
                    WHERE artist_id IS NOT NULL;

                CREATE INDEX IF NOT EXISTS "IX_track_collaborator_track_id_display_order"
                    ON billing.track_collaborator (track_id, display_order);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS billing.track_collaborator CASCADE;

                CREATE TABLE IF NOT EXISTS billing.release_collaborator (
                    release_id uuid NOT NULL,
                    artist_id uuid NOT NULL,
                    role catalog.release_collaborator_role NOT NULL,
                    display_order integer NOT NULL,
                    CONSTRAINT "PK_release_collaborator" PRIMARY KEY (release_id, artist_id, role),
                    CONSTRAINT "FK_release_collaborator_artist_artist_id" FOREIGN KEY (artist_id)
                        REFERENCES billing.artist (id) ON DELETE RESTRICT,
                    CONSTRAINT "FK_release_collaborator_release_release_id" FOREIGN KEY (release_id)
                        REFERENCES billing.release (id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_release_collaborator_artist_id"
                    ON billing.release_collaborator (artist_id);

                CREATE INDEX IF NOT EXISTS "IX_release_collaborator_release_id_display_order"
                    ON billing.release_collaborator (release_id, display_order);
                """);
        }
    }
}
