using Matrix.Population.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Persistence;

public sealed class EducationEconomicEffectsMigrationTests
{
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
