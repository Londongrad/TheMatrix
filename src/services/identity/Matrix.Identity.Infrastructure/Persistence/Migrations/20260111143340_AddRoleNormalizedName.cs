using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleNormalizedName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Добавляем колонку временно nullable
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Roles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // 2) Заполняем существующие строки
            migrationBuilder.Sql("""
                                     UPDATE "Roles"
                                     SET "NormalizedName" = upper(trim("Name"))
                                     WHERE "NormalizedName" IS NULL;
                                 """);

            // 3) Делаем NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Roles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            // 4) Удаляем старый unique индекс на Name
            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            // 5) Создаём unique индекс на NormalizedName
            migrationBuilder.CreateIndex(
                name: "ux_roles_normalized_name",
                table: "Roles",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_roles_normalized_name",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Roles");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);
        }
    }
}
