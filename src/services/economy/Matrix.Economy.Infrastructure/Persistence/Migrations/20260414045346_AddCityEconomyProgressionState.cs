using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityEconomyProgressionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityEconomyProgressionStates",
                columns: table => new
                {
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_completed_tick_id = table.Column<long>(type: "bigint", nullable: false),
                    last_processed_date = table.Column<DateTime>(type: "date", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityEconomyProgressionStates", x => x.city_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityEconomyProgressionStates");
        }
    }
}
