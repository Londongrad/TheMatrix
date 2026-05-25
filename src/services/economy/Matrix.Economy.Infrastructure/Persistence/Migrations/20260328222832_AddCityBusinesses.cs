using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityBusinesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "City_Business",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    unit_kind = table.Column<string>(type: "text", nullable: false),
                    unit_code = table.Column<string>(type: "text", nullable: false),
                    unit_display_name = table.Column<string>(type: "text", nullable: false),
                    unit_symbol = table.Column<string>(type: "text", nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_reserve_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_capital_injections_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_retail_turnover_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_net_sales_revenue_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_operating_expenses_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_tax_remitted_amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Business", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "City_Business_Ledger_Entry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    reference_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Business_Ledger_Entry", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_city_id",
                table: "City_Business",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_city_id_name",
                table: "City_Business",
                columns: new[] { "city_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_Ledger_Entry_business_id_occurred_at_utc",
                table: "City_Business_Ledger_Entry",
                columns: new[] { "business_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_Ledger_Entry_city_id",
                table: "City_Business_Ledger_Entry",
                column: "city_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "City_Business");

            migrationBuilder.DropTable(
                name: "City_Business_Ledger_Entry");
        }
    }
}
