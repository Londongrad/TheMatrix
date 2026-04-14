using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingResupplyFocusDistricts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PendingResupplyFocusDistrictId",
                table: "CityStockpiles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingResupplyFocusDistrictId",
                table: "CityStockpiles");
        }
    }
}
