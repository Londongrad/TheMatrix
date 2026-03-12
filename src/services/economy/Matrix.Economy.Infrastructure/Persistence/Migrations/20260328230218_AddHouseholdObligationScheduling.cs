using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdObligationScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_cadence",
                table: "City_Household_Obligation",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_charge_due_at_utc",
                table: "City_Household_Obligation",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Obligation_city_id_next_charge_due_at_utc",
                table: "City_Household_Obligation",
                columns: new[] { "city_id", "next_charge_due_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_Household_Obligation_city_id_next_charge_due_at_utc",
                table: "City_Household_Obligation");

            migrationBuilder.DropColumn(
                name: "billing_cadence",
                table: "City_Household_Obligation");

            migrationBuilder.DropColumn(
                name: "next_charge_due_at_utc",
                table: "City_Household_Obligation");
        }
    }
}
