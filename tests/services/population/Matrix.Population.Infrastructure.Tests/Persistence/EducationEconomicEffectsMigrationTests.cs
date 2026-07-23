using Matrix.Population.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Persistence;

public sealed class EducationEconomicEffectsMigrationTests
{
    [Fact]
    public void AttendanceMigration_PreservesMissingResultsAsNull()
    {
        using var db = new PopulationDbContext(new DbContextOptionsBuilder<PopulationDbContext>()
            .UseNpgsql("Host=localhost;Database=design_only;Username=design_only").Options);
        string sql = db.GetService<IMigrator>().GenerateScript("20260902060034_AddEducationEconomicEffects",
            "20260902071106_AddEducationAttendanceObservation");
        Assert.Contains("ADD \"AttendanceIndex\" numeric(9,4);", sql);
        Assert.Contains("ADD \"AttendanceSourceTickId\" bigint;", sql);
        Assert.Contains("ADD \"AttendanceObservedAtSimTimeUtc\" timestamp with time zone;", sql);
        Assert.DoesNotContain("UPDATE \"EducationParticipationProjections\"", sql);
    }

    [Fact]
    public void ModelMatchesMigration_AndLegacyRowsKeepNullableEffects()
    {
        using var db = new PopulationDbContext(new DbContextOptionsBuilder<PopulationDbContext>()
            .UseNpgsql("Host=localhost;Database=design_only;Username=design_only").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        string sql = db.GetService<IMigrator>().GenerateScript("20260715122900_AddEducationParticipationProjection",
            "20260902060034_AddEducationEconomicEffects");
        Assert.Contains("ADD \"EconomicEffectsJson\" text;", sql);
    }
}
