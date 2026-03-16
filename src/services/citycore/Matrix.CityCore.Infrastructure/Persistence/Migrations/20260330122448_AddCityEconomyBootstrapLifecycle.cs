using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityEconomyBootstrapLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EconomyBootstrapCompletedAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EconomyBootstrapFailedAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EconomyBootstrapFailureCode",
                table: "Cities",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EconomyBootstrapOperationId",
                table: "Cities",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.Sql(
                sql: """
                     ALTER TABLE "Cities"
                     ALTER COLUMN "EconomyBootstrapOperationId" DROP DEFAULT;
                     """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EconomyBootstrapCompletedAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "EconomyBootstrapFailedAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "EconomyBootstrapFailureCode",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "EconomyBootstrapOperationId",
                table: "Cities");
        }
    }
}
