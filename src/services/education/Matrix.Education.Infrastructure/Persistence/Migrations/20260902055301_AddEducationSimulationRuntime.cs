using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationSimulationRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "education_simulation_runtimes",
                columns: table => new
                {
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_key = table.Column<string>(type: "text", nullable: false),
                    host_type_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_simulation_runtimes", x => x.simulation_host_id);
                });

            // Before this migration Education supported only Classic City. Preserve those
            // existing hosts, including paused worlds; new hosts must supply their runtime.
            migrationBuilder.Sql("""
                INSERT INTO education_simulation_runtimes (simulation_host_id, scenario_key, host_type_key)
                SELECT legacy.simulation_host_id, 'classic-city', 'city'
                FROM (
                    SELECT simulation_host_id FROM education_student_profiles
                    UNION SELECT simulation_host_id FROM education_institutions
                    UNION SELECT simulation_host_id FROM education_progression_checkpoints
                ) AS legacy
                WHERE NOT EXISTS (
                    SELECT 1 FROM education_simulation_deletions AS deleted
                    WHERE deleted.simulation_host_id = legacy.simulation_host_id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "education_simulation_runtimes");
        }
    }
}
