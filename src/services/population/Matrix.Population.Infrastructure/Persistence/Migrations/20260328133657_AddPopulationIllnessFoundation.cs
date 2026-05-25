using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPopulationIllnessFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentIllnessKind",
                table: "Persons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentIllnessSeverity",
                table: "Persons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IllnessDiagnosedOn",
                table: "Persons",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastIllnessRecoveredOn",
                table: "Persons",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentIllnessKind",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "CurrentIllnessSeverity",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "IllnessDiagnosedOn",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "LastIllnessRecoveredOn",
                table: "Persons");
        }
    }
}
