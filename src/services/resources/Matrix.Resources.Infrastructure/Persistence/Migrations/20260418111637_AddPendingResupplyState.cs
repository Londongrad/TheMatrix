using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingResupplyState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingResupplyFocus",
                table: "CityStockpiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingResupplyIntensity",
                table: "CityStockpiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingResupplyIsScheduled",
                table: "CityStockpiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingResupplyReadyAtTickId",
                table: "CityStockpiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingResupplyFocus",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "PendingResupplyIntensity",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "PendingResupplyIsScheduled",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "PendingResupplyReadyAtTickId",
                table: "CityStockpiles");
        }
    }
}
