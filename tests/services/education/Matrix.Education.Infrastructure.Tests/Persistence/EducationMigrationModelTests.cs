using Matrix.Education.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence;

public sealed class EducationMigrationModelTests
{
    [Fact]
    public void RuntimeMigration_MatchesModelAndBackfillsOnlyExistingNonDeletedHosts()
    {
        using var db = new EducationDbContext(new DbContextOptionsBuilder<EducationDbContext>()
            .UseNpgsql("Host=localhost;Database=design_only;Username=design_only").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        string sql = db.GetService<IMigrator>().GenerateScript("20260714204900_AddEducationOutbox",
            "20260902055301_AddEducationSimulationRuntime");
        Assert.Contains("CREATE TABLE education_simulation_runtimes", sql);
        Assert.Contains("FROM education_student_profiles", sql);
        Assert.Contains("FROM education_institutions", sql);
        Assert.Contains("FROM education_progression_checkpoints", sql);
        Assert.Contains("WHERE NOT EXISTS", sql);
        Assert.Contains("education_simulation_deletions", sql);
    }
}
