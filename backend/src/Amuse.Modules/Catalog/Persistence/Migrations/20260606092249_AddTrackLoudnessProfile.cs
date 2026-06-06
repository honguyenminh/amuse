using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amuse.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackLoudnessProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "loudness_analyzed_at",
                schema: "catalog",
                table: "track",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "loudness_integrated_lufs",
                schema: "catalog",
                table: "track",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "loudness_linear_gain_lu",
                schema: "catalog",
                table: "track",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "loudness_range_lu",
                schema: "catalog",
                table: "track",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "loudness_target_integrated_lufs",
                schema: "catalog",
                table: "track",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "loudness_target_true_peak_dbtp",
                schema: "catalog",
                table: "track",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "loudness_threshold_lufs",
                schema: "catalog",
                table: "track",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "loudness_true_peak_dbtp",
                schema: "catalog",
                table: "track",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "loudness_analyzed_at",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "loudness_integrated_lufs",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "loudness_linear_gain_lu",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "loudness_range_lu",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "loudness_target_integrated_lufs",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "loudness_target_true_peak_dbtp",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "loudness_threshold_lufs",
                schema: "catalog",
                table: "track");

            migrationBuilder.DropColumn(
                name: "loudness_true_peak_dbtp",
                schema: "catalog",
                table: "track");
        }
    }
}
