using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityHouseholdObligationDelinquencyLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "eviction_eligible_at_utc",
                table: "City_Household_Obligation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "eviction_notice_issued_at_utc",
                table: "City_Household_Obligation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "first_missed_charge_due_at_utc",
                table: "City_Household_Obligation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_charge_attempted_at_utc",
                table: "City_Household_Obligation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "missed_charge_count",
                table: "City_Household_Obligation",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "service_cutoff_at_utc",
                table: "City_Household_Obligation",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "eviction_eligible_at_utc",
                table: "City_Household_Obligation");

            migrationBuilder.DropColumn(
                name: "eviction_notice_issued_at_utc",
                table: "City_Household_Obligation");

            migrationBuilder.DropColumn(
                name: "first_missed_charge_due_at_utc",
                table: "City_Household_Obligation");

            migrationBuilder.DropColumn(
                name: "last_charge_attempted_at_utc",
                table: "City_Household_Obligation");

            migrationBuilder.DropColumn(
                name: "missed_charge_count",
                table: "City_Household_Obligation");

            migrationBuilder.DropColumn(
                name: "service_cutoff_at_utc",
                table: "City_Household_Obligation");
        }
    }
}
