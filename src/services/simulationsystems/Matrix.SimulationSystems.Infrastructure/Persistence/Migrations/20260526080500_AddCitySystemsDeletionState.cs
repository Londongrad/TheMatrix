using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCitySystemsDeletionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CitySystemsDeletionStates",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitySystemsDeletionStates", x => x.CityId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CitySystemsDeletionStates_DeletedAtUtc",
                table: "CitySystemsDeletionStates",
                column: "DeletedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CitySystemsDeletionStates");
        }
    }
}
