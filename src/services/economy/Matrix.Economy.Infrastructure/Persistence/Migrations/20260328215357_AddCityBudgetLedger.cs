using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityBudgetLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "total_direct_revenue_amount",
                table: "City_Budget",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "City_Budget_Ledger_Entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    reference_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Budget_Ledger_Entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_Ledger_Entries_city_id_occurred_at_utc",
                table: "City_Budget_Ledger_Entries",
                columns: new[] { "city_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_Ledger_Entries_reference_code",
                table: "City_Budget_Ledger_Entries",
                column: "reference_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "City_Budget_Ledger_Entries");

            migrationBuilder.DropColumn(
                name: "total_direct_revenue_amount",
                table: "City_Budget");
        }
    }
}
