using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityProvisioningLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PopulationBootstrapCompletedAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PopulationBootstrapError",
                table: "Cities",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PopulationBootstrapFailedAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PopulationBootstrapCompletedAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "PopulationBootstrapError",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "PopulationBootstrapFailedAtUtc",
                table: "Cities");
        }
    }
}
