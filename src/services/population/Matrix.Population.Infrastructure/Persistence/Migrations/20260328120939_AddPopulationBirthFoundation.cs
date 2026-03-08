using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPopulationBirthFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FatherId",
                table: "Persons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastChildbirthDate",
                table: "Persons",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MotherId",
                table: "Persons",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FatherId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "LastChildbirthDate",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "MotherId",
                table: "Persons");
        }
    }
}
