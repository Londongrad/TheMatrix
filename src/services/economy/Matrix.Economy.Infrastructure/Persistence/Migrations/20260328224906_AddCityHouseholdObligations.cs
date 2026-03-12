using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityHouseholdObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "City_Household_Account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    external_reference_code = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    unit_kind = table.Column<string>(type: "text", nullable: false),
                    unit_code = table.Column<string>(type: "text", nullable: false),
                    unit_display_name = table.Column<string>(type: "text", nullable: false),
                    unit_symbol = table.Column<string>(type: "text", nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_opening_balance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_payroll_income_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_consumer_spending_amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Household_Account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "City_Household_Account_Ledger_Entry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    reference_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Household_Account_Ledger_Entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "City_Household_Obligation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    unit_kind = table.Column<string>(type: "text", nullable: false),
                    unit_code = table.Column<string>(type: "text", nullable: false),
                    unit_display_name = table.Column<string>(type: "text", nullable: false),
                    unit_symbol = table.Column<string>(type: "text", nullable: false),
                    charge_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    last_charged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    charge_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Household_Obligation", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_city_id",
                table: "City_Household_Account",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_city_id_name",
                table: "City_Household_Account",
                columns: new[] { "city_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_external_reference_code",
                table: "City_Household_Account",
                column: "external_reference_code");

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_Ledger_Entry_city_id",
                table: "City_Household_Account_Ledger_Entry",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_Ledger_Entry_household_account_id_oc~",
                table: "City_Household_Account_Ledger_Entry",
                columns: new[] { "household_account_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Obligation_city_id",
                table: "City_Household_Obligation",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Obligation_household_account_id",
                table: "City_Household_Obligation",
                column: "household_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Obligation_household_account_id_provider_bus~",
                table: "City_Household_Obligation",
                columns: new[] { "household_account_id", "provider_business_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Obligation_provider_business_id",
                table: "City_Household_Obligation",
                column: "provider_business_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "City_Household_Account");

            migrationBuilder.DropTable(
                name: "City_Household_Account_Ledger_Entry");

            migrationBuilder.DropTable(
                name: "City_Household_Obligation");
        }
    }
}
