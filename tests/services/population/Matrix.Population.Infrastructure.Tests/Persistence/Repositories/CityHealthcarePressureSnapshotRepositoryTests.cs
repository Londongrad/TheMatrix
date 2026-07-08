using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityHealthcarePressureSnapshotRepositoryTests
{
    [Fact]
    public async Task UpsertAndGetByCity_PersistsLatestProjection()
    {
        await using PopulationTestDatabase database =
            PopulationInfrastructureTestSupport.CreateDbContext();
        var repository = new CityHealthcarePressureSnapshotRepository(database.DbContext);
        var cityId = CityId.From(Guid.NewGuid());

        await repository.UpsertAsync(CreateSnapshot(cityId, sourceRevision: 17, activeIllnessCount: 8));
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();
        await repository.UpsertAsync(CreateSnapshot(cityId, sourceRevision: 18, activeIllnessCount: 6));
        await database.DbContext.SaveChangesAsync();
        database.DbContext.ChangeTracker.Clear();

        ClassicCityHealthcarePressureSnapshot? stored = await repository.GetByCityAsync(cityId);

        Assert.NotNull(stored);
        Assert.Equal(18, stored.SourceRevision);
        Assert.Equal(6, stored.Pressure.ActiveIllnessCount);
        Assert.Equal(100, stored.PatientCount);
    }

    private static ClassicCityHealthcarePressureSnapshot CreateSnapshot(
        CityId cityId,
        long sourceRevision,
        int activeIllnessCount)
    {
        return new ClassicCityHealthcarePressureSnapshot(
            cityId,
            sourceRevision,
            new DateOnly(2048, 5, 6),
            PatientCount: 100,
            new CityPopulationHealthcarePressureProfile(
                activeIllnessCount,
                SevereIllnessCount: 2,
                MedicalLoadIndex: 0.82m,
                TriagePressureIndex: 0.34m,
                RecoverySupportIndex: 1.12m),
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc: new DateTimeOffset(2048, 5, 6, 10, 1, 0, TimeSpan.Zero));
    }
}
