using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyLedgerPaginationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_Household_Account_Ledger_Entry_household_account_id_oc~",
                table: "City_Household_Account_Ledger_Entry");

            migrationBuilder.DropIndex(
                name: "IX_City_Business_Ledger_Entry_business_id_occurred_at_utc",
                table: "City_Business_Ledger_Entry");

            migrationBuilder.DropIndex(
                name: "IX_City_Budget_Ledger_Entries_city_id_occurred_at_utc",
                table: "City_Budget_Ledger_Entries");

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_Ledger_Entry_household_account_id_oc~",
                table: "City_Household_Account_Ledger_Entry",
                columns: new[] { "household_account_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_Ledger_Entry_business_id_occurred_at_utc_id",
                table: "City_Business_Ledger_Entry",
                columns: new[] { "business_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_Ledger_Entries_city_id_occurred_at_utc_id",
                table: "City_Budget_Ledger_Entries",
                columns: new[] { "city_id", "occurred_at_utc", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_Household_Account_Ledger_Entry_household_account_id_oc~",
                table: "City_Household_Account_Ledger_Entry");

            migrationBuilder.DropIndex(
                name: "IX_City_Business_Ledger_Entry_business_id_occurred_at_utc_id",
                table: "City_Business_Ledger_Entry");

            migrationBuilder.DropIndex(
                name: "IX_City_Budget_Ledger_Entries_city_id_occurred_at_utc_id",
                table: "City_Budget_Ledger_Entries");

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_Ledger_Entry_household_account_id_oc~",
                table: "City_Household_Account_Ledger_Entry",
                columns: new[] { "household_account_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_Ledger_Entry_business_id_occurred_at_utc",
                table: "City_Business_Ledger_Entry",
                columns: new[] { "business_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_Ledger_Entries_city_id_occurred_at_utc",
                table: "City_Budget_Ledger_Entries",
                columns: new[] { "city_id", "occurred_at_utc" });
        }
    }
}
