using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PopulationDbContext))]
    [Migration("20260719131100_NormalizeEducationProjectionStages")]
    public sealed class NormalizeEducationProjectionStages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "EducationParticipationProjections"
                SET "ActiveStage" = lower(btrim("ActiveStage"))
                WHERE "ActiveStage" IS NOT NULL;

                UPDATE "EducationParticipationProjections"
                SET "CompletedStage" = lower(btrim("CompletedStage"))
                WHERE "CompletedStage" IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
