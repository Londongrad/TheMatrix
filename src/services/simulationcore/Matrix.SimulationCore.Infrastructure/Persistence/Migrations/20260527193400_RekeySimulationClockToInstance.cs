using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RekeySimulationClockToInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clock identifiers already contain SimulationId values; only their ownership changes.
            migrationBuilder.DropForeignKey(
                name: "FK_SimulationClocks_Cities_Id",
                table: "SimulationClocks");

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationClocks_SimulationInstances_Id",
                table: "SimulationClocks",
                column: "Id",
                principalTable: "SimulationInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimulationClocks_SimulationInstances_Id",
                table: "SimulationClocks");

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationClocks_Cities_Id",
                table: "SimulationClocks",
                column: "Id",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
