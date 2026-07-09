using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientHouseholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "household_id",
                table: "healthcare_patient_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_healthcare_profiles_host_household",
                table: "healthcare_patient_profiles",
                columns: new[] { "simulation_host_id", "household_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_healthcare_profiles_host_household",
                table: "healthcare_patient_profiles");

            migrationBuilder.DropColumn(
                name: "household_id",
                table: "healthcare_patient_profiles");
        }
    }
}
