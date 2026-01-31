using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityProvisioningLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PopulationBootstrapCompletedAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PopulationBootstrapFailureCode",
                table: "Cities",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PopulationBootstrapFailedAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PopulationBootstrapOperationId",
                table: "Cities",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Cities"
                SET
                    "PopulationBootstrapOperationId" = "Id",
                    "PopulationBootstrapCompletedAtUtc" = CASE
                        WHEN "Status" IN (1, 2) THEN "CreatedAtUtc"
                        ELSE "PopulationBootstrapCompletedAtUtc"
                    END
                WHERE "PopulationBootstrapOperationId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "PopulationBootstrapOperationId",
                table: "Cities",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PopulationBootstrapCompletedAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "PopulationBootstrapFailureCode",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "PopulationBootstrapFailedAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "PopulationBootstrapOperationId",
                table: "Cities");
        }
    }
}
