using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityProvisioningCorrelationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProvisioningCorrelationId",
                table: "Cities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_ProvisioningCorrelationId",
                table: "Cities",
                column: "ProvisioningCorrelationId",
                unique: true,
                filter: "\"ProvisioningCorrelationId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cities_ProvisioningCorrelationId",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ProvisioningCorrelationId",
                table: "Cities");
        }
    }
}
