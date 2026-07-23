using Matrix.Population.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Persistence;

public sealed class EducationDailyRoutineMigrationTests
{
    [Fact]
    public void Migration_MatchesModelAndPreservesLegacyPayloadDistinction()
    {
        using var db = new PopulationDbContext(new DbContextOptionsBuilder<PopulationDbContext>()
            .UseNpgsql("Host=localhost;Database=design_only;Username=design_only").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        string sql = db.GetService<IMigrator>().GenerateScript("20260902071106_AddEducationAttendanceObservation",
            "20260902075100_AddEducationDailyRoutine");
        Assert.Contains("ADD \"RoutineJson\" text;", sql);
        Assert.DoesNotContain("UPDATE \"EducationParticipationProjections\"", sql);
        Assert.DoesNotContain("DROP", sql);
    }
}
