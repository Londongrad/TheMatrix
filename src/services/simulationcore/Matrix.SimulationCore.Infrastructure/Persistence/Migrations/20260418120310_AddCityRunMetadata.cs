using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityRunMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RunId",
                table: "Cities",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "ScenarioModelSetVersion",
                table: "Cities",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "classic-city-v1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RunId",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ScenarioModelSetVersion",
                table: "Cities");
        }
    }
}
