using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyLedgerIdempotencyKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_Ledger_Entry_household_account_id_ki~",
                table: "City_Household_Account_Ledger_Entry",
                columns: new[] { "household_account_id", "kind", "reference_code" },
                unique: true,
                filter: "\"reference_code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_Ledger_Entry_business_id_kind_reference_code",
                table: "City_Business_Ledger_Entry",
                columns: new[] { "business_id", "kind", "reference_code" },
                unique: true,
                filter: "\"reference_code\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_Household_Account_Ledger_Entry_household_account_id_ki~",
                table: "City_Household_Account_Ledger_Entry");

            migrationBuilder.DropIndex(
                name: "IX_City_Business_Ledger_Entry_business_id_kind_reference_code",
                table: "City_Business_Ledger_Entry");
        }
    }
}
