using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityBudgetAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "City_Budget_Allocation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    unit_kind = table.Column<string>(type: "text", nullable: false),
                    unit_code = table.Column<string>(type: "text", nullable: false),
                    unit_display_name = table.Column<string>(type: "text", nullable: false),
                    unit_symbol = table.Column<string>(type: "text", nullable: false),
                    target_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_spent_amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Budget_Allocation", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_Allocation_city_id",
                table: "City_Budget_Allocation",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_Allocation_city_id_category",
                table: "City_Budget_Allocation",
                columns: new[] { "city_id", "category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "City_Budget_Allocation");
        }
    }
}
