using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCareDeliveryActivityTotals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "acute_care_delivery_count",
                table: "healthcare_patient_health_progression_batch_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "current_date",
                table: "healthcare_patient_health_progression_batch_sets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "emergency_care_delivery_count",
                table: "healthcare_patient_health_progression_batch_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "processed_patient_count",
                table: "healthcare_patient_health_progression_batch_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "recorded_care_delivery_batch_count",
                table: "healthcare_patient_health_progression_batch_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "routine_care_delivery_count",
                table: "healthcare_patient_health_progression_batch_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "urgent_care_delivery_count",
                table: "healthcare_patient_health_progression_batch_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE healthcare_patient_health_progression_batch_sets
                SET current_date = (first_received_at_utc AT TIME ZONE 'UTC')::date,
                    recorded_care_delivery_batch_count = received_batch_count;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "acute_care_delivery_count",
                table: "healthcare_patient_health_progression_batch_sets");

            migrationBuilder.DropColumn(
                name: "current_date",
                table: "healthcare_patient_health_progression_batch_sets");

            migrationBuilder.DropColumn(
                name: "emergency_care_delivery_count",
                table: "healthcare_patient_health_progression_batch_sets");

            migrationBuilder.DropColumn(
                name: "processed_patient_count",
                table: "healthcare_patient_health_progression_batch_sets");

            migrationBuilder.DropColumn(
                name: "recorded_care_delivery_batch_count",
                table: "healthcare_patient_health_progression_batch_sets");

            migrationBuilder.DropColumn(
                name: "routine_care_delivery_count",
                table: "healthcare_patient_health_progression_batch_sets");

            migrationBuilder.DropColumn(
                name: "urgent_care_delivery_count",
                table: "healthcare_patient_health_progression_batch_sets");
        }
    }
}
