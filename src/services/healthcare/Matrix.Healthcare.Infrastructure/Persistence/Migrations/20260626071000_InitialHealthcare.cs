using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialHealthcare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "healthcare_patient_profiles",
                columns: table => new
                {
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    birth_date = table.Column<DateTime>(type: "date", nullable: false),
                    sex = table.Column<int>(type: "integer", nullable: false),
                    is_alive = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_source_revision = table.Column<long>(type: "bigint", nullable: false),
                    last_synchronized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_healthcare_patient_profiles", x => x.patient_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_healthcare_profiles_tick_candidates",
                table: "healthcare_patient_profiles",
                columns: new[] { "simulation_host_id", "is_active", "is_alive", "birth_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "healthcare_patient_profiles");
        }
    }
}
