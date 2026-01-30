using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPersonToValueObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "Persons");

            migrationBuilder.AddColumn<Guid>(
                name: "SpouseId",
                table: "Persons",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpouseId",
                table: "Persons");

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                table: "Persons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
