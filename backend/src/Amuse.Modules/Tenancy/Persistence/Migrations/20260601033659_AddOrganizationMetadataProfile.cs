using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Tenancy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMetadataProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "country_code",
                schema: "tenancy",
                table: "organization",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "tenancy",
                table: "organization",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "imprint_name",
                schema: "tenancy",
                table: "organization",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website_url",
                schema: "tenancy",
                table: "organization",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "country_code",
                schema: "tenancy",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "tenancy",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "imprint_name",
                schema: "tenancy",
                table: "organization");

            migrationBuilder.DropColumn(
                name: "website_url",
                schema: "tenancy",
                table: "organization");
        }
    }
}
