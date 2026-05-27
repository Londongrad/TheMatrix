using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimulationInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    HostTypeKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Seed = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProvisioningCorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationInstances", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "SimulationInstances" (
                    "Id",
                    "HostId",
                    "ScenarioKey",
                    "HostTypeKey",
                    "Seed",
                    "RunId",
                    "ModelVersion",
                    "ProvisioningCorrelationId",
                    "State",
                    "CreatedAtUtc",
                    "ArchivedAtUtc")
                SELECT
                    "Id",
                    "Id",
                    'classic-city',
                    'city',
                    "GenerationSeed",
                    "RunId",
                    "ScenarioModelSetVersion",
                    "ProvisioningCorrelationId",
                    "Status",
                    "CreatedAtUtc",
                    "ArchivedAtUtc"
                FROM "Cities";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationInstances_CreatedAtUtc",
                table: "SimulationInstances",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationInstances_ProvisioningCorrelationId",
                table: "SimulationInstances",
                column: "ProvisioningCorrelationId",
                unique: true,
                filter: "\"ProvisioningCorrelationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationInstances_ScenarioKey_HostTypeKey_HostId",
                table: "SimulationInstances",
                columns: new[] { "ScenarioKey", "HostTypeKey", "HostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationInstances_State",
                table: "SimulationInstances",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimulationInstances");
        }
    }
}
